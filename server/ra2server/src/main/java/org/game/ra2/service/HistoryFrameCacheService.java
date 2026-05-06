package org.game.ra2.service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.game.ra2.util.ObjectMapperProvider;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.RejectedExecutionException;
import java.util.concurrent.ThreadFactory;
import java.util.concurrent.ThreadPoolExecutor;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * 历史帧缓存读取服务，负责按文件名异步读取历史帧文件，并合并同一文件的并发请求。
 * 服务会短期缓存成功结果和文件不存在结果，用于降低客户端重复请求时的磁盘访问压力。
 */
public class HistoryFrameCacheService {
    private static final Logger logger = LogManager.getLogger(HistoryFrameCacheService.class);
    private static final HistoryFrameCacheService INSTANCE = new HistoryFrameCacheService();
    private static final int CORE_POOL_SIZE = 4;
    private static final int MAXIMUM_POOL_SIZE = 10;
    private static final int QUEUE_CAPACITY = 20;
    private static final long KEEP_ALIVE_TIME_SECONDS = 60;
    private static final long SUCCESS_CACHE_TTL_MILLIS = 60 * 1000;
    private static final long FILE_NOT_FOUND_CACHE_TTL_MILLIS = 5 * 1000;
    private static final long SHUTDOWN_TIMEOUT_SECONDS = 10;
    private static final String REQUEST_TYPE = "getHistoryFrames";
    private static final String START_TYPE = "historyFramesStart";
    private static final String FRAME_TYPE = "historyFramesFrame";
    private static final String FAILED_TYPE = "historyFramesFailed";
    private static final String FRAMES_FIELD = "frames";
    private static final String END_FLAG = "isEnd";
    private static final int MAX_FRAMES_PER_MESSAGE = 10;
    private static final String REASON_INVALID_FILE_NAME = "invalid_file_name";
    private static final String REASON_FILE_NOT_FOUND = "file_not_found";
    private static final String REASON_READ_FAILED = "read_failed";
    private static final String REASON_SERVICE_SHUTDOWN = "service_shutdown";
    private static final Path HISTORY_FRAME_DIRECTORY = Paths.get("data", "history-frames");

    private final ObjectMapper objectMapper = ObjectMapperProvider.getInstance();
    private final ThreadPoolExecutor executor;
    private final Object lock = new Object();
    private final Map<String, CacheEntry> cache = new HashMap<>();
    private final Map<String, List<String>> loadingQueues = new HashMap<>();
    private final AtomicBoolean shutdownStarted = new AtomicBoolean(false);

    /**
     * 创建历史帧缓存读取服务，并初始化专用读取线程池。
     */
    private HistoryFrameCacheService() {
        executor = new ThreadPoolExecutor(
                CORE_POOL_SIZE,
                MAXIMUM_POOL_SIZE,
                KEEP_ALIVE_TIME_SECONDS,
                TimeUnit.SECONDS,
                new LinkedBlockingQueue<>(QUEUE_CAPACITY),
                new HistoryFrameReadThreadFactory(),
                new ThreadPoolExecutor.CallerRunsPolicy());
    }

    /**
     * 获取历史帧缓存读取服务单例。
     *
     * @return 历史帧缓存读取服务实例
     */
    public static HistoryFrameCacheService getInstance() {
        return INSTANCE;
    }

    /**
     * 判断消息类型是否为历史帧文件获取请求。
     *
     * @param type WebSocket消息类型
     * @return true表示是历史帧文件获取请求，false表示不是
     */
    public boolean isHistoryFrameRequest(String type) {
        return REQUEST_TYPE.equals(type);
    }

    /**
     * 处理客户端历史帧文件获取请求。
     *
     * @param channelId 请求客户端频道ID
     * @param request 客户端请求JSON
     */
    public void handleRequest(String channelId, JsonNode request) {
        String fileName = getFileName(request);
        if (!isValidFileName(fileName)) {
            sendFailed(channelId, fileName, REASON_INVALID_FILE_NAME);
            return;
        }

        CacheEntry hitEntry = null;
        synchronized (lock) {
            CacheEntry cacheEntry = cache.get(fileName);
            if (cacheEntry != null) {
                if (!cacheEntry.isExpired()) {
                    cacheEntry.extend();
                    hitEntry = cacheEntry;
                } else {
                    cache.remove(fileName);
                }
            }

            if (hitEntry == null) {
                List<String> waitingChannels = loadingQueues.get(fileName);
                if (waitingChannels != null) {
                    waitingChannels.add(channelId);
                    return;
                }

                waitingChannels = new ArrayList<>();
                waitingChannels.add(channelId);
                loadingQueues.put(fileName, waitingChannels);
            }
        }

        if (hitEntry != null) {
            sendCacheEntry(channelId, fileName, hitEntry);
            return;
        }

        submitLoadTask(fileName);
    }

    /**
     * 优雅关闭历史帧缓存读取线程池。
     */
    public void shutdownGracefully() {
        if (!shutdownStarted.compareAndSet(false, true)) {
            return;
        }

        executor.shutdown();
        try {
            if (!executor.awaitTermination(SHUTDOWN_TIMEOUT_SECONDS, TimeUnit.SECONDS)) {
                logger.warn("历史帧缓存读取线程池未在{}秒内关闭，开始强制关闭", SHUTDOWN_TIMEOUT_SECONDS);
                executor.shutdownNow();
                if (!executor.awaitTermination(SHUTDOWN_TIMEOUT_SECONDS, TimeUnit.SECONDS)) {
                    logger.error("历史帧缓存读取线程池强制关闭后仍未终止");
                }
            }
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            executor.shutdownNow();
            logger.error("等待历史帧缓存读取线程池关闭时被中断", e);
        }
    }

    /**
     * 从请求JSON中读取历史帧文件名。
     *
     * @param request 客户端请求JSON
     * @return 请求中的文件名，缺失时返回null
     */
    private String getFileName(JsonNode request) {
        JsonNode data = request.get("data");
        if (data == null || !data.has("fileName")) {
            return null;
        }
        return data.get("fileName").asText(null);
    }

    /**
     * 校验历史帧文件名是否只包含基础JSON文件名。
     *
     * @param fileName 待校验的文件名
     * @return true表示文件名合法，false表示文件名非法
     */
    private boolean isValidFileName(String fileName) {
        return fileName != null
                && !fileName.trim().isEmpty()
                && fileName.endsWith(".json")
                && !fileName.contains("/")
                && !fileName.contains("\\")
                && Paths.get(fileName).getFileName().toString().equals(fileName);
    }

    /**
     * 提交历史帧文件加载任务。
     *
     * @param fileName 待加载的历史帧文件名
     */
    private void submitLoadTask(String fileName) {
        if (shutdownStarted.get()) {
            finishLoading(fileName, CacheEntry.failed(REASON_SERVICE_SHUTDOWN), false);
            return;
        }

        try {
            executor.execute(() -> loadFile(fileName));
        } catch (RejectedExecutionException e) {
            logger.warn("历史帧缓存读取服务拒绝加载文件：{}", fileName, e);
            finishLoading(fileName, CacheEntry.failed(REASON_SERVICE_SHUTDOWN), false);
        }
    }

    /**
     * 在线程池中加载历史帧文件。
     *
     * @param fileName 待加载的历史帧文件名
     */
    private void loadFile(String fileName) {
        Path filePath = HISTORY_FRAME_DIRECTORY.resolve(fileName).normalize();
        try {
            if (!Files.exists(filePath)) {
                finishLoading(fileName, CacheEntry.failed(REASON_FILE_NOT_FOUND), true);
                return;
            }

            JsonNode data = objectMapper.readTree(filePath.toFile());
            finishLoading(fileName, CacheEntry.success(data), true);
            logger.info("历史帧文件加载完成：{}", filePath);
        } catch (IOException e) {
            logger.error("历史帧文件读取失败：{}", filePath, e);
            finishLoading(fileName, CacheEntry.failed(REASON_READ_FAILED), false);
        }
    }

    /**
     * 完成文件加载并通知等待该文件的全部客户端。
     *
     * @param fileName 已加载的历史帧文件名
     * @param cacheEntry 加载结果缓存项
     * @param shouldCache true表示需要缓存该结果，false表示只通知当前等待队列
     */
    private void finishLoading(String fileName, CacheEntry cacheEntry, boolean shouldCache) {
        List<String> waitingChannels;
        synchronized (lock) {
            waitingChannels = loadingQueues.remove(fileName);
            if (shouldCache) {
                cacheEntry.extend();
                cache.put(fileName, cacheEntry);
            }
        }

        if (waitingChannels == null) {
            return;
        }

        for (String channelId : waitingChannels) {
            sendCacheEntry(channelId, fileName, cacheEntry);
        }
    }

    /**
     * 根据缓存项类型发送成功或失败响应。
     *
     * @param channelId 客户端频道ID
     * @param fileName 历史帧文件名
     * @param cacheEntry 缓存项
     */
    private void sendCacheEntry(String channelId, String fileName, CacheEntry cacheEntry) {
        if (cacheEntry.isSuccess()) {
            sendSuccess(channelId, fileName, cacheEntry.getData());
        } else {
            sendFailed(channelId, fileName, cacheEntry.getReason());
        }
    }

    /**
     * 发送历史帧文件读取成功响应。
     *
     * @param channelId 客户端频道ID
     * @param fileName 历史帧文件名
     * @param data 历史帧文件JSON内容
     */
    private void sendSuccess(String channelId, String fileName, JsonNode data) {
        try {
            JsonNode frames = data.get(FRAMES_FIELD);
            if (frames == null || !frames.isArray() || frames.size() == 0) {
                sendStart(channelId, fileName, data, true);
                return;
            }

            sendStart(channelId, fileName, data, false);
            for (int startIndex = 0; startIndex < frames.size(); startIndex += MAX_FRAMES_PER_MESSAGE) {
                int endIndex = Math.min(startIndex + MAX_FRAMES_PER_MESSAGE, frames.size());
                sendFrameBatch(channelId, fileName, frames, startIndex, endIndex, endIndex >= frames.size());
            }
        } catch (Exception e) {
            logger.error("发送历史帧文件成功响应失败 - channelId: {}, fileName: {}", channelId, fileName, e);
        }
    }

    /**
     * 发送历史帧文件开始响应，响应中只包含历史帧文件的非帧元数据。
     *
     * @param channelId 客户端频道ID
     * @param fileName 历史帧文件名
     * @param data 历史帧文件JSON内容
     * @param isEnd true表示没有后续帧批次，false表示后续还会发送帧批次
     */
    private void sendStart(String channelId, String fileName, JsonNode data, boolean isEnd) {
        ObjectNode response = objectMapper.createObjectNode();
        response.put("type", START_TYPE);
        response.put("fileName", fileName);
        response.set("data", createMetadata(data));
        response.put(END_FLAG, isEnd);
        WebSocketSessionManager.getInstance().sendMessage(channelId, writeResponse(response));
    }

    /**
     * 发送历史帧批次响应，单个响应最多携带固定数量的帧。
     *
     * @param channelId 客户端频道ID
     * @param fileName 历史帧文件名
     * @param frames 历史帧数组
     * @param startIndex 批次起始下标，包含该下标
     * @param endIndex 批次结束下标，不包含该下标
     * @param isEnd true表示当前批次是最后一批，false表示后续还有帧批次
     */
    private void sendFrameBatch(String channelId, String fileName, JsonNode frames, int startIndex, int endIndex, boolean isEnd) {
        ArrayNode batch = objectMapper.createArrayNode();
        for (int index = startIndex; index < endIndex; index++) {
            batch.add(frames.get(index));
        }

        ObjectNode response = objectMapper.createObjectNode();
        response.put("type", FRAME_TYPE);
        response.put("fileName", fileName);
        response.set(FRAMES_FIELD, batch);
        response.put(END_FLAG, isEnd);
        WebSocketSessionManager.getInstance().sendMessage(channelId, writeResponse(response));
    }

    /**
     * 复制历史帧文件中的非帧字段，生成开始响应需要发送的元数据。
     *
     * @param data 历史帧文件JSON内容
     * @return 不包含frames字段的元数据JSON对象
     */
    private ObjectNode createMetadata(JsonNode data) {
        ObjectNode metadata = objectMapper.createObjectNode();
        Iterator<Map.Entry<String, JsonNode>> fields = data.fields();
        while (fields.hasNext()) {
            Map.Entry<String, JsonNode> field = fields.next();
            if (!FRAMES_FIELD.equals(field.getKey())) {
                metadata.set(field.getKey(), field.getValue());
            }
        }
        return metadata;
    }

    /**
     * 将响应JSON对象序列化成WebSocket可发送的字符串。
     *
     * @param response 响应JSON对象
     * @return 序列化后的JSON字符串
     */
    private String writeResponse(ObjectNode response) {
        try {
            return objectMapper.writeValueAsString(response);
        } catch (Exception e) {
            throw new IllegalStateException("历史帧响应序列化失败", e);
        }
    }

    /**
     * 发送历史帧文件读取失败响应。
     *
     * @param channelId 客户端频道ID
     * @param fileName 历史帧文件名
     * @param reason 失败原因
     */
    private void sendFailed(String channelId, String fileName, String reason) {
        try {
            ObjectNode response = objectMapper.createObjectNode();
            response.put("type", FAILED_TYPE);
            if (fileName != null) {
                response.put("fileName", fileName);
            }
            response.put("reason", reason);
            WebSocketSessionManager.getInstance().sendMessage(channelId, objectMapper.writeValueAsString(response));
        } catch (Exception e) {
            logger.error("发送历史帧文件失败响应失败 - channelId: {}, fileName: {}, reason: {}", channelId, fileName, reason, e);
        }
    }

    /**
     * 历史帧文件读取缓存项，保存成功读取的JSON内容或可缓存的失败原因。
     */
    private static class CacheEntry {
        private final JsonNode data;
        private final String reason;
        private long expireAtMillis;

        /**
         * 创建历史帧文件读取缓存项。
         *
         * @param data 成功读取的历史帧JSON，失败项为空
         * @param reason 失败原因，成功项为空
         */
        private CacheEntry(JsonNode data, String reason) {
            this.data = data;
            this.reason = reason;
        }

        /**
         * 创建成功缓存项。
         *
         * @param data 成功读取的历史帧JSON
         * @return 成功缓存项
         */
        private static CacheEntry success(JsonNode data) {
            return new CacheEntry(data, null);
        }

        /**
         * 创建失败缓存项。
         *
         * @param reason 失败原因
         * @return 失败缓存项
         */
        private static CacheEntry failed(String reason) {
            return new CacheEntry(null, reason);
        }

        /**
         * 判断缓存项是否为成功结果。
         *
         * @return true表示成功结果，false表示失败结果
         */
        private boolean isSuccess() {
            return reason == null;
        }

        /**
         * 判断缓存项是否已经过期。
         *
         * @return true表示缓存已过期，false表示缓存仍有效
         */
        private boolean isExpired() {
            return System.currentTimeMillis() >= expireAtMillis;
        }

        /**
         * 将缓存项过期时间延长到对应缓存时长，成功结果缓存一分钟，文件不存在结果缓存五秒。
         */
        private void extend() {
            long ttlMillis = REASON_FILE_NOT_FOUND.equals(reason)
                    ? FILE_NOT_FOUND_CACHE_TTL_MILLIS
                    : SUCCESS_CACHE_TTL_MILLIS;
            expireAtMillis = System.currentTimeMillis() + ttlMillis;
        }

        /**
         * 获取成功读取的历史帧JSON。
         *
         * @return 历史帧JSON内容
         */
        private JsonNode getData() {
            return data;
        }

        /**
         * 获取失败原因。
         *
         * @return 失败原因
         */
        private String getReason() {
            return reason;
        }
    }

    /**
     * 历史帧文件读取线程工厂，用于给读取线程设置易识别的线程名。
     */
    private static class HistoryFrameReadThreadFactory implements ThreadFactory {
        private final AtomicInteger threadNumber = new AtomicInteger(1);

        /**
         * 创建新的历史帧文件读取线程。
         *
         * @param runnable 线程执行任务
         * @return 已命名的读取线程
         */
        @Override
        public Thread newThread(Runnable runnable) {
            Thread thread = new Thread(runnable);
            thread.setName("Game-HistoryFrameRead-" + threadNumber.getAndIncrement());
            return thread;
        }
    }
}
