package org.game.ra2.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.game.ra2.util.ObjectMapperProvider;
import org.game.ra2.entity.RoomType;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Queue;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.concurrent.LinkedBlockingQueue;

/**
 * 匹配服务类
 */
public class MatchService {

    private static final Logger logger = LogManager.getLogger(MatchService.class);
    private static final String DISCONNECT_MESSAGE_TYPE = "disconnect";

    // 添加PlayerInfo内部类
    public static class PlayerInfo {
        private String channelId;
        private String name;

        public PlayerInfo(String channelId, String name) {
            this.channelId = channelId;
            this.name = name;
        }

        public String getChannelId() {
            return channelId;
        }

        public String getName() {
            return name;
        }
    }

    private static MatchService instance = new MatchService();
    private final LinkedBlockingQueue<Message> messageQueue = new LinkedBlockingQueue<>();
    // 为每种房间类型维护一个等待队列
    private final Map<RoomType, Queue<PlayerInfo>> waitingPlayersByType = new ConcurrentHashMap<>();

    private MatchService() {
        // 初始化所有房间类型的队列
        for (RoomType type : RoomType.values()) {
            waitingPlayersByType.put(type, new ConcurrentLinkedQueue<>());
        }
        
        // 启动匹配处理线程
        Thread matchThread = new Thread(this::run);
        matchThread.setName("MatchThread");
        matchThread.start();
    }

    public static MatchService getInstance() {
        return instance;
    }

    /**
     * 添加匹配请求到队列
     * @param channelId
     * @param data
     */
    public void addMessage(String channelId, JsonNode data) {
        try {
            logger.info("添加消息队列: {}, channelId={}", data.toString(), channelId);
            Message message = new Message(channelId, data);
            messageQueue.put(message);
        } catch (InterruptedException e) {
            logger.error("添加匹配请求时被中断", e);
        } catch (Exception e) {
            logger.error("添加匹配请求时发生错误", e);
        }
    }

    /**
     * 处理匹配队列
     */
    private void run() {
        logger.info("匹配线程启动");

        while (true) {
            try {
                // 处理匹配队列中的消息
                processMessage();

                // 心跳逻辑 (每秒20帧)
                Thread.sleep(50); // 50ms = 1/20秒

            } catch (InterruptedException e) {
                logger.info("匹配线程被中断");
                break;
            } catch (Exception e) {
                logger.error("匹配线程发生未预期错误", e);
            }
        }
    }

    private void processMessage() {
        // 一次性获取所有消息
        List<Message> messages = new ArrayList<>();
        messageQueue.drainTo(messages);

        // 遍历所有消息，根据消息类型处理
        for (Message message : messages) {
            JsonNode data = message.getData();
            String type = data.has("type") ? data.get("type").asText() : "";

            switch (type) {
                case "match":
                    handleMatchMessage(message);
                    break;
                case "cancelMatch":
                    handleCancelMatchMessage(message);
                    break;
                case DISCONNECT_MESSAGE_TYPE:
                    handleDisconnectMessage(message);
                    break;
                default:
                    // 处理其他类型的消息
                    logger.warn("未知消息类型: {}, message:{}", type, message);
                    break;
            }
        }

        // 处理匹配逻辑
        processMatching();
    }

    /**
     * 处理匹配消息
     * @param message
     */
    private void handleMatchMessage(Message message) {
        // 将消息数据转换为Player并保存到等待玩家列表中
        JsonNode data = message.getData().get("data");
        String name = data.has("name") ? data.get("name").asText() : "Unknown";
        
        // 获取房间类型，默认为双人
        String roomTypeStr = data.has("roomType") ? data.get("roomType").asText() : "DUO";
        RoomType roomType;
        try {
            roomType = RoomType.valueOf(roomTypeStr.toUpperCase());
        } catch (IllegalArgumentException e) {
            logger.warn("无效的房间类型: {}，使用默认类型 DUO", roomTypeStr);
            roomType = RoomType.DUO;
        }

        // 检查channelId是否已经在匹配中
        if (isMatching(message.getChannelId())) {
            logger.info("玩家已存在匹配中: {}", message.getChannelId());
            return;
        }

        // 检查channelId已经在房间中
        if (WebSocketSessionManager.getInstance().isChannelInRoom(message.getChannelId())) {
            logger.info("玩家已加入房间: {}", message.getChannelId());
            return;
        }

        PlayerInfo player = new PlayerInfo(message.getChannelId(), name); // 创建内部PlayerInfo对象
        Queue<PlayerInfo> queue = waitingPlayersByType.get(roomType);
        queue.add(player);
        logger.info("添加玩家到 {} 等待列表: {}", roomType, message.getChannelId());

        try {
            Map<String, String> response = new HashMap<>();
            response.put("type", "matched");
            String jsonResponse = ObjectMapperProvider.getInstance().writeValueAsString(response);

            WebSocketSessionManager.getInstance().sendMessage(message.getChannelId(), jsonResponse);
        } catch (JsonProcessingException e) {
            logger.error("处理匹配消息时序列化响应失败", e);
        }

    }

    /**
     * 处理取消匹配消息
     * @param message 取消匹配消息
     */
    private void handleCancelMatchMessage(Message message) {
        String channelId = message.getChannelId();

        if (WebSocketSessionManager.getInstance().isChannelInRoom(channelId)) {
            logger.info("玩家已进入房间，无法取消匹配: {}", channelId);
            sendCancelMatchResponse(channelId, false, "already_in_room");
            return;
        }

        boolean removed = removeWaitingPlayer(channelId);
        if (removed) {
            logger.info("玩家取消匹配成功: {}", channelId);
            sendCancelMatchResponse(channelId, true, null);
        } else {
            logger.info("玩家不在匹配队列中，无法取消匹配: {}", channelId);
            sendCancelMatchResponse(channelId, false, "not_matching");
        }
    }

    /**
     * 处理断线消息
     * @param message 断线消息
     */
    private void handleDisconnectMessage(Message message) {
        String channelId = message.getChannelId();
        boolean removed = removeWaitingPlayer(channelId);
        if (removed) {
            logger.info("断线玩家已从匹配队列移除: {}", channelId);
        } else {
            logger.info("断线玩家不在匹配队列中: {}", channelId);
        }
    }

    /**
     * 发送取消匹配结果
     * @param channelId 频道ID
     * @param success 是否取消成功
     * @param reason 失败原因，成功时为空
     */
    private void sendCancelMatchResponse(String channelId, boolean success, String reason) {
        try {
            Map<String, Object> response = new HashMap<>();
            response.put("type", "matchCancelled");
            response.put("success", success);
            if (reason != null && !reason.isEmpty()) {
                response.put("reason", reason);
            }

            String jsonResponse = ObjectMapperProvider.getInstance().writeValueAsString(response);
            WebSocketSessionManager.getInstance().sendMessage(channelId, jsonResponse);
        } catch (JsonProcessingException e) {
            logger.error("处理取消匹配消息时序列化响应失败", e);
        }
    }

    private boolean isMatching(String channelId) {
        // 检查所有队列中是否已经存在该玩家
        for (Queue<PlayerInfo> queue : waitingPlayersByType.values()) {
            if (queue.stream().anyMatch(player -> player.getChannelId().equals(channelId))) {
                return true;
            }
        }
        return false;
    }

    /**
     * 从全部房间类型的等待队列中移除玩家
     * @param channelId 频道ID
     * @return 是否找到并移除了等待中的玩家
     */
    private boolean removeWaitingPlayer(String channelId) {
        boolean removed = false;
        for (Queue<PlayerInfo> queue : waitingPlayersByType.values()) {
            PlayerInfo playerToRemove = null;
            for (PlayerInfo player : queue) {
                if (player.getChannelId().equals(channelId)) {
                    playerToRemove = player;
                    break;
                }
            }
            if (playerToRemove != null) {
                queue.remove(playerToRemove);
                removed = true;
            }
        }
        return removed;
    }

    /**
     * 处理匹配逻辑
     */
    private void processMatching() {
        // 遍历所有房间类型处理匹配
        for (RoomType roomType : RoomType.values()) {
            Queue<PlayerInfo> queue = waitingPlayersByType.get(roomType);
            
            // 当队列中的玩家数量满足房间要求时创建房间
            while (queue.size() >= roomType.getMaxPlayers()) {
                // 创建RoomService实例来创建房间
                RoomService roomService = RoomServiceManager.getInstance().createRoomService();
                
                // 根据房间类型创建相应数量的玩家
                PlayerInfo[] players = new PlayerInfo[roomType.getMaxPlayers()];
                for (int i = 0; i < roomType.getMaxPlayers(); i++) {
                    players[i] = queue.poll();
                }
                
                roomService.createRoom(players);
            }
        }
    }

    /**
     * 处理断线逻辑
     * @param channelId
     */
    public void handleDisconnect(String channelId) {
        try {
            logger.info("添加断线消息到匹配队列: {}", channelId);
            ObjectNode data = ObjectMapperProvider.getInstance().createObjectNode();
            data.put("type", DISCONNECT_MESSAGE_TYPE);
            Message message = new Message(channelId, data);
            messageQueue.put(message);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            logger.error("添加断线消息时被中断", e);
        } catch (Exception e) {
            logger.error("添加断线消息时发生错误", e);
        }
    }
}
