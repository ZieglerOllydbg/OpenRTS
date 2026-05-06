package org.game.ra2.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.apache.commons.lang3.RandomUtils;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.game.ra2.util.ObjectMapperProvider;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.List;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.ThreadFactory;
import java.util.concurrent.ThreadPoolExecutor;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * 历史帧持久化服务，负责接收房间销毁时生成的历史帧快照，并通过独立线程池异步写入本地文件。
 * 服务内部维护有界任务队列和优雅关闭逻辑，避免持久化任务阻塞房间线程的主循环。
 */
public class HistoryFramePersistenceService {
    private static final Logger logger = LogManager.getLogger(HistoryFramePersistenceService.class);
    private static final HistoryFramePersistenceService INSTANCE = new HistoryFramePersistenceService();
    private static final int CORE_POOL_SIZE = 4;
    private static final int MAXIMUM_POOL_SIZE = 10;
    private static final int QUEUE_CAPACITY = 20;
    private static final long KEEP_ALIVE_TIME_SECONDS = 60;
    private static final long SHUTDOWN_TIMEOUT_SECONDS = 10;
    private static final Path HISTORY_FRAME_DIRECTORY = Paths.get("data", "history-frames");

    private final ObjectMapper objectMapper = ObjectMapperProvider.getInstance();
    private final ThreadPoolExecutor executor;
    private final AtomicBoolean shutdownStarted = new AtomicBoolean(false);

    private HistoryFramePersistenceService() {
        executor = new ThreadPoolExecutor(
                CORE_POOL_SIZE,
                MAXIMUM_POOL_SIZE,
                KEEP_ALIVE_TIME_SECONDS,
                TimeUnit.SECONDS,
                new LinkedBlockingQueue<>(QUEUE_CAPACITY),
                new HistoryFrameThreadFactory(),
                new ThreadPoolExecutor.CallerRunsPolicy());
    }

    /**
     * 获取历史帧持久化服务单例。
     *
     * @return 历史帧持久化服务实例
     */
    public static HistoryFramePersistenceService getInstance() {
        return INSTANCE;
    }

    /**
     * 异步提交房间历史帧持久化任务。
     *
     * @param fileName 持久化文件名
     * @param roomId 房间ID
     * @param playerInfos 房间玩家快照
     * @param frameInfos 历史帧快照
     * @return true表示任务已投递或由调用线程执行，false表示线程池已进入关闭流程
     */
    public boolean submit(String fileName, String roomId, List<PlayerInfo> playerInfos, List<FrameInfo> frameInfos) {
        if (shutdownStarted.get()) {
            logger.warn("历史帧持久化服务已关闭，拒绝房间 {} 的历史帧持久化任务，文件名：{}", roomId, fileName);
            return false;
        }

        executor.execute(() -> persist(fileName, roomId, playerInfos, frameInfos));
        return true;
    }

    /**
     * 生成历史帧持久化文件名。
     *
     * @param playerCount 房间玩家数量
     * @return 全局唯一概率极高的JSON文件名
     */
    public String generateFileName(int playerCount) {
        long timestamp = System.currentTimeMillis();
        long random = RandomUtils.nextLong(100000000L, 999999999L);
        return String.format("history_frames_%d_%d_%d.json", playerCount, timestamp, random);
    }

    /**
     * 优雅关闭历史帧持久化线程池。
     */
    public void shutdownGracefully() {
        if (!shutdownStarted.compareAndSet(false, true)) {
            return;
        }

        executor.shutdown();
        try {
            if (!executor.awaitTermination(SHUTDOWN_TIMEOUT_SECONDS, TimeUnit.SECONDS)) {
                logger.warn("历史帧持久化线程池未在{}秒内关闭，开始强制关闭", SHUTDOWN_TIMEOUT_SECONDS);
                executor.shutdownNow();
                if (!executor.awaitTermination(SHUTDOWN_TIMEOUT_SECONDS, TimeUnit.SECONDS)) {
                    logger.error("历史帧持久化线程池强制关闭后仍未终止");
                }
            }
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            executor.shutdownNow();
            logger.error("等待历史帧持久化线程池关闭时被中断", e);
        }
    }

    private void persist(String fileName, String roomId, List<PlayerInfo> playerInfos, List<FrameInfo> frameInfos) {
        try {
            Files.createDirectories(HISTORY_FRAME_DIRECTORY);

            ObjectNode root = objectMapper.createObjectNode();
            root.put("fileName", fileName);
            root.put("roomId", roomId);
            root.put("playerCount", playerInfos.size());

            ArrayNode players = objectMapper.createArrayNode();
            for (PlayerInfo playerInfo : playerInfos) {
                ObjectNode player = objectMapper.createObjectNode();
                player.put("campId", playerInfo.getCampId());
                player.put("name", playerInfo.getName());
                player.put("channelId", playerInfo.getChannelId());
                player.put("channelValid", playerInfo.isChannelValid());
                players.add(player);
            }
            root.set("players", players);

            ArrayNode frames = objectMapper.createArrayNode();
            for (FrameInfo frameInfo : frameInfos) {
                ObjectNode frame = objectMapper.createObjectNode();
                frame.put("frame", frameInfo.getFrame());
                frame.put("message", frameInfo.getMessage());
                frames.add(frame);
            }
            root.set("frames", frames);

            Path outputPath = HISTORY_FRAME_DIRECTORY.resolve(fileName);
            objectMapper.writerWithDefaultPrettyPrinter().writeValue(outputPath.toFile(), root);
            logger.info("房间 {} 历史帧持久化完成，文件：{}，玩家数：{}，历史帧数：{}",
                    roomId, outputPath, playerInfos.size(), frameInfos.size());
        } catch (IOException e) {
            logger.error("房间 {} 历史帧持久化失败，文件名：{}", roomId, fileName, e);
        }
    }

    /**
     * 玩家基础信息快照，记录历史帧文件中需要保留的玩家身份和连接状态。
     */
    public static class PlayerInfo {
        private final int campId;
        private final String name;
        private final String channelId;
        private final boolean channelValid;

        /**
         * 创建玩家基础信息快照。
         *
         * @param campId 阵营ID
         * @param name 玩家名称
         * @param channelId 玩家当前频道ID
         * @param channelValid 玩家频道是否有效
         */
        public PlayerInfo(int campId, String name, String channelId, boolean channelValid) {
            this.campId = campId;
            this.name = name;
            this.channelId = channelId;
            this.channelValid = channelValid;
        }

        /**
         * 获取阵营ID。
         *
         * @return 阵营ID
         */
        public int getCampId() {
            return campId;
        }

        /**
         * 获取玩家名称。
         *
         * @return 玩家名称
         */
        public String getName() {
            return name;
        }

        /**
         * 获取玩家当前频道ID。
         *
         * @return 玩家当前频道ID
         */
        public String getChannelId() {
            return channelId;
        }

        /**
         * 获取玩家频道是否有效。
         *
         * @return true表示频道有效，false表示频道已失效
         */
        public boolean isChannelValid() {
            return channelValid;
        }
    }

    /**
     * 历史帧信息快照，保存帧号和该帧广播给客户端的原始同步消息。
     */
    public static class FrameInfo {
        private final int frame;
        private final String message;

        /**
         * 创建历史帧信息快照。
         *
         * @param frame 帧号
         * @param message 帧同步原始消息
         */
        public FrameInfo(int frame, String message) {
            this.frame = frame;
            this.message = message;
        }

        /**
         * 获取帧号。
         *
         * @return 帧号
         */
        public int getFrame() {
            return frame;
        }

        /**
         * 获取帧同步原始消息。
         *
         * @return 帧同步原始消息
         */
        public String getMessage() {
            return message;
        }
    }

    private static class HistoryFrameThreadFactory implements ThreadFactory {
        private final AtomicInteger threadNumber = new AtomicInteger(1);

        @Override
        public Thread newThread(Runnable runnable) {
            Thread thread = new Thread(runnable);
            thread.setName("Game-HistoryFrame-" + threadNumber.getAndIncrement());
            return thread;
        }
    }
}
