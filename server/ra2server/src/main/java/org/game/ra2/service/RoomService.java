package org.game.ra2.service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.game.ra2.util.ObjectMapperProvider;
import org.game.ra2.entity.Camp;
import org.game.ra2.entity.Player;
import org.game.ra2.thread.Room;
import org.game.ra2.thread.RoomThread;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.LinkedBlockingQueue;

/**
 * 房间服务类
 */
public class RoomService {
    private static final Logger logger = LogManager.getLogger(RoomService.class);
    private static final String DISCONNECT_MESSAGE_TYPE = "disconnect";
    private static final String RECONNECT_MESSAGE_TYPE = "reconnect";
    private static final long PING_TIMEOUT_MILLIS = 15 * 1000;
    private static final long PING_TIMEOUT_CHECK_INTERVAL_MILLIS = 1000;
    
    private final RoomThread roomThread;
    private final ObjectMapper objectMapper = ObjectMapperProvider.getInstance();
    private final LinkedBlockingQueue<Message> messageQueue = new LinkedBlockingQueue<>();

    private final String roomId;
    // 房间管理数据结构
    private Room room;
    private boolean destroyed = false; // 标记房间是否已被销毁
    private long lastPingTimeoutCheckTime = 0;

    public RoomService(String roomId, RoomThread roomThread) {
        this.roomId = roomId;
        this.roomThread = roomThread;
    }

    /**
     * 添加消息到队列
     * @param channelId
     * @param data
     */
    public void addMessage(String channelId, JsonNode data) {
        try {
            // 如果房间已被销毁，则忽略消息
            if (destroyed) {
                return;
            }
            
            Message message = new Message(channelId, data);
            messageQueue.put(message);
        } catch (InterruptedException e) {
            logger.error("添加消息到队列时被中断", e);
        } catch (Exception e) {
            logger.error("添加消息到队列时发生错误", e);
        }
    }

    /**
     * 获取房间ID
     * @return
     */
    public String getRoomId() {
        return roomId;
    }

    /**
     * 创建房间（支持多种房间类型）
     * @param players 玩家数组
     */
    public void createRoom(MatchService.PlayerInfo[] players) {
        if (room != null) {
            logger.error("房间已存在");
            return;
        }

        // 创建房间
        room = new Room(roomId);

        // 根据玩家数量确定阵营分配方式
        Camp[] camps;
        switch (players.length) {
            case 1:
                camps = new Camp[]{Camp.Red};
                break;
            case 2:
                camps = new Camp[]{Camp.Red, Camp.Blue};
                break;
            case 3:
                camps = new Camp[]{Camp.Red, Camp.Blue, Camp.Green};
                break;
            case 4:
                camps = new Camp[]{Camp.Red, Camp.Blue, Camp.Green, Camp.Yellow};
                break;
            case 8:
                camps = new Camp[]{Camp.Red, Camp.Blue, Camp.Green, Camp.Yellow, 
                                  Camp.Orange, Camp.Purple, Camp.Pink, Camp.Brown};
                break;
            default:
                // 默认分配前N个阵营
                camps = new Camp[players.length];
                Camp[] allCamps = Camp.values();
                for (int i = 0; i < players.length && i < allCamps.length; i++) {
                    camps[i] = allCamps[i];
                }
                break;
        }

        // 添加玩家到房间
        for (int i = 0; i < players.length; i++) {
            Player player = new Player(camps[i]);
            player.setChannelId(players[i].getChannelId());
            player.setName(players[i].getName());
            room.addPlayer(player);
            
            // 在WebSocketSessionManager中记录映射关系
            WebSocketSessionManager.getInstance().setChannelRoomMapping(players[i].getChannelId(), roomId);
        }
        
        logger.info("创建房间: {}，玩家数量：{}", roomId, players.length);

        // 发送给房间内的所有玩家
        for (Player player : room.getPlayers()) {
            if (player.isChannelValid()) {
                notifyMatchSuccess(player);
            } else {
                logger.warn("无法向玩家[{}]发送匹配成功消息, 因为已断线！", player);
            }
        }
    }

    /**
     * 创建双人房间（兼容旧版本）
     * @param player1
     * @param player2
     */
    public void createRoom(MatchService.PlayerInfo player1, MatchService.PlayerInfo player2) {
        createRoom(new MatchService.PlayerInfo[]{player1, player2});
    }
    
    private void notifyMatchSuccess(Player sendPlayer) {
        try {
            ObjectNode response = objectMapper.createObjectNode();
            response.put("type", "matchSuccess");
            // 设置房间ID
            response.put("roomId", roomId);
            // 设置玩家campID
            response.put("yourCampId", sendPlayer.getCamp().getId());
            // 设置token
            response.put("yourToken", sendPlayer.getToken());
            
            ArrayNode dataArray = objectMapper.createArrayNode();
            for (Player player : room.getPlayers()) {
                ObjectNode playerNode = objectMapper.createObjectNode();
                playerNode.put("campId", player.getCamp().getId());
                playerNode.put("name", player.getName());
                dataArray.add(playerNode);
            }
            
            response.set("data", dataArray);
            
            // 添加初始游戏状态 - 根据玩家数量选择不同方法
            ObjectNode initialState;
            if (room.getPlayers().size() == 2) {
                initialState = createInitialGameState2Player();
            } else {
                initialState = createInitialGameState();
            }
            response.set("initialState", initialState);
            
            String message = objectMapper.writeValueAsString(response);

            WebSocketSessionManager.getInstance().sendMessage(sendPlayer.getChannelId(), message);

            logger.info("向玩家[{}]发送匹配成功消息: {}", sendPlayer, message);
        } catch (Exception e) {
            logger.error("发送匹配成功消息时发生错误: {}", e.getMessage(), e);
        }
    }

    /**
     * 处理断线
     * @param channelId
     */
    public void handleDisconnect(String channelId) {
        try {
            if (destroyed) {
                return;
            }

            ObjectNode data = objectMapper.createObjectNode();
            data.put("type", DISCONNECT_MESSAGE_TYPE);
            Message message = new Message(channelId, data);
            messageQueue.put(message);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            logger.error("添加房间断线消息时被中断", e);
        } catch (Exception e) {
            logger.error("添加房间断线消息时发生错误", e);
        }
    }

    /**
     * 处理重连请求
     * @param channelId 新连接频道ID
     * @param data 重连请求数据
     */
    public void handleReconnect(String channelId, JsonNode data) {
        try {
            if (destroyed) {
                sendReconnectFailed(channelId, "room_destroyed");
                return;
            }

            Message message = new Message(channelId, data);
            messageQueue.put(message);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            logger.error("添加房间重连消息时被中断", e);
        } catch (Exception e) {
            logger.error("添加房间重连消息时发生错误", e);
        }
    }
    
    /**
     * 玩家离开房间
     * @param channelId
     */
    public void handlePlayerLeave(String channelId) {
        if (room != null) {
            room.removePlayer(channelId);
            WebSocketSessionManager.getInstance().removeChannelRoomMapping(channelId);
        }
    }
    
    /**
     * 停止服务
     */
    public void stopService() {
        roomThread.removeRoomService(roomId);
    }
    
    /**
     * 获取房间线程
     * @return
     */
    public RoomThread getRoomThread() {
        return roomThread;
    }
    

    
    /**
     * 处理消息队列
     */
    public void processMessageQueue() {
        // 一次性获取所有消息
        List<Message> messages = new ArrayList<>();
        messageQueue.drainTo(messages);

        // 遍历所有消息，根据消息类型处理
        for (Message message : messages) {
            JsonNode data = message.getData();
            String type = data.has("type") ? data.get("type").asText() : "";
            String channelId = message.getChannelId();

            switch (type) {
                case "ready":
                    room.markPlayerReady(channelId);
                    break;
                case "frameInput":
                    room.addFrameInput(channelId, data);
                    break;
                case "leave":
                    handlePlayerLeave(channelId);
                    break;
                case DISCONNECT_MESSAGE_TYPE:
                    room.handleDisconnect(channelId);
                    break;
                case RECONNECT_MESSAGE_TYPE:
                    handlePlayerReconnect(channelId, data);
                    break;
                default:
                    // 处理其他类型的消息
                    logger.warn("房间未知消息类型: {}", type);
                    break;
            }
        }
    }

    public void pulse() {
        long currentTime = System.currentTimeMillis();
        if (room != null && !destroyed && currentTime - lastPingTimeoutCheckTime >= PING_TIMEOUT_CHECK_INTERVAL_MILLIS) {
            lastPingTimeoutCheckTime = currentTime;
            checkPingTimeout();
        }

        if (room != null && room.isGameStarted()) {
            room.update();
        }
        
        // 检查房间是否应该销毁
        if (room != null && !destroyed && room.shouldDestroy()) {
            destroyRoom();
        }
    }

    /**
     * 玩家重连房间
     * @param channelId 新连接频道ID
     * @param data 重连请求数据
     */
    private void handlePlayerReconnect(String channelId, JsonNode data) {
        JsonNode reconnectData = data.get("data");
        if (reconnectData == null || !reconnectData.has("token")) {
            sendReconnectFailed(channelId, "token_invalid");
            return;
        }

        String token = reconnectData.get("token").asText();
        int lastFrame = reconnectData.has("lastFrame") ? reconnectData.get("lastFrame").asInt(-1) : -1;

        Room.ReconnectResult result = room.handleReconnect(channelId, token);
        if (!result.isSuccess()) {
            sendReconnectFailed(channelId, "token_invalid");
            return;
        }

        WebSocketSessionManager sessionManager = WebSocketSessionManager.getInstance();
        String oldChannelId = result.getOldChannelId();
        if (oldChannelId != null && !oldChannelId.equals(channelId)) {
            sessionManager.removeChannelRoomMapping(oldChannelId);
        }
        sessionManager.setChannelRoomMapping(channelId, roomId);
        sessionManager.updatePingTime(channelId);

        sendReconnectSuccess(channelId, result.getPlayer().getCamp().getId());
        sendFrameSyncHistory(channelId, lastFrame);
    }

    /**
     * 发送重连成功响应
     * @param channelId 频道ID
     * @param campId 玩家阵营ID
     */
    private void sendReconnectSuccess(String channelId, int campId) {
        try {
            ObjectNode response = objectMapper.createObjectNode();
            response.put("type", "reconnectSuccess");
            response.put("roomId", roomId);
            response.put("yourCampId", campId);
            response.put("currentFrame", room.getCurrentFrame());

            WebSocketSessionManager.getInstance().sendMessage(channelId, objectMapper.writeValueAsString(response));
        } catch (Exception e) {
            logger.error("发送重连成功消息时发生错误", e);
        }
    }

    /**
     * 发送重连失败响应
     * @param channelId 频道ID
     * @param reason 失败原因
     */
    public void sendReconnectFailed(String channelId, String reason) {
        try {
            ObjectNode response = objectMapper.createObjectNode();
            response.put("type", "reconnectFailed");
            response.put("reason", reason);

            WebSocketSessionManager.getInstance().sendMessage(channelId, objectMapper.writeValueAsString(response));
        } catch (Exception e) {
            logger.error("发送重连失败消息时发生错误", e);
        }
    }

    /**
     * 补发历史帧同步消息
     * @param channelId 频道ID
     * @param lastFrame 客户端已处理的最后帧
     */
    private void sendFrameSyncHistory(String channelId, int lastFrame) {
        List<String> messages = room.getFrameSyncMessagesAfter(lastFrame);
        for (String message : messages) {
            WebSocketSessionManager.getInstance().sendMessage(channelId, message);
        }
        logger.info("房间 {} 向频道 {} 补发{}条历史帧同步消息，lastFrame={}", roomId, channelId, messages.size(), lastFrame);
    }

    /**
     * 检查房间内玩家ping是否超时
     */
    private void checkPingTimeout() {
        WebSocketSessionManager sessionManager = WebSocketSessionManager.getInstance();
        for (Player player : room.getPlayers()) {
            String channelId = player.getChannelId();
            if (player.isChannelValid() && sessionManager.isPingTimeout(channelId, PING_TIMEOUT_MILLIS)) {
                logger.warn("房间 {} 玩家 {} ping超过{}毫秒未更新，主动关闭连接", roomId, player, PING_TIMEOUT_MILLIS);
                sessionManager.closeChannel(channelId);
            }
        }
    }
    
    /**
     * 销毁房间
     */
    private void destroyRoom() {
        logger.info("正在销毁房间: {}", roomId);
        destroyed = true;
        room.destroy();
        
        // 通知RoomServiceManager移除此房间服务
        RoomServiceManager.getInstance().removeRoomService(roomId);
    }
    
    /**
     * 创建初始游戏状态 (适用于2人游戏，地图大小为128*128)
     * @return 包含所有阵营初始状态的ObjectNode
     */
    private ObjectNode createInitialGameState2Player() {
        ObjectNode initialState = objectMapper.createObjectNode();
        
        // 为每个阵营创建初始单位 (仅适用于2个玩家)
        Player[] players = room.getPlayers().toArray(new Player[0]);
        for (int p = 0; p < players.length && p < 2; p++) {
            Player player = players[p];
            int campId = player.getCamp().getId();
            Camp camp = player.getCamp();
            ObjectNode campState = objectMapper.createObjectNode();
            
            // 创建建筑物数组
            ArrayNode buildings = objectMapper.createArrayNode();
            
            // 添加主要基地 (没有坦克工厂)
            ObjectNode base = objectMapper.createObjectNode();
            base.put("id", "base_" + campId);
            base.put("type", "base");
            
            // 设置基地位置 (在128*128场景中)
            // 左下角和右上角分别放置两个队伍
            if (camp == Camp.Red) { // 红色阵营 - 左下角
                base.put("x", 20);
                base.put("y", 20);
            } else if (camp == Camp.Blue) { // 蓝色阵营 - 右上角
                base.put("x", 108);
                base.put("y", 108);
            } else { // 其他阵营默认位置
                base.put("x", 64);
                base.put("y", 64);
            }
            
            buildings.add(base);
            
            campState.set("buildings", buildings);
            
            // 创建单位数组
            ArrayNode units = objectMapper.createArrayNode();
            
            // 添加3辆坦克
            for (int i = 1; i <= 3; i++) {
                ObjectNode tank = objectMapper.createObjectNode();
                tank.put("id", "tank_" + campId + "_" + i);
                tank.put("type", "tank");
                
                // 设置坦克位置，围绕基地分布 (在128*128场景中)
                if (camp == Camp.Red) { // 红色阵营
                    switch (i) {
                        case 1: 
                            tank.put("x", 15);
                            tank.put("y", 20);
                            break;
                        case 2:
                            tank.put("x", 20);
                            tank.put("y", 15);
                            break;
                        case 3:
                            tank.put("x", 25);
                            tank.put("y", 20);
                            break;
                    }
                } else if (camp == Camp.Blue) { // 蓝色阵营
                    switch (i) {
                        case 1:
                            tank.put("x", 103);
                            tank.put("y", 108);
                            break;
                        case 2:
                            tank.put("x", 108);
                            tank.put("y", 113);
                            break;
                        case 3:
                            tank.put("x", 113);
                            tank.put("y", 108);
                            break;
                    }
                } else { // 其他阵营
                    tank.put("x", 56 + i * 8);
                    tank.put("y", 64);
                }
                
                units.add(tank);
            }
            
            // 添加初始资金2200
            campState.put("money", 2200);
            
            campState.set("units", units);
            initialState.set(String.valueOf(campId), campState);
        }
        
        // 添加中央中立资源点
        ObjectNode neutralResources = objectMapper.createObjectNode();
        ArrayNode neutralBuildings = objectMapper.createArrayNode();
        
        // 添加各阵营的起始资源点到中立资源中
        // 红色阵营资源点
        ObjectNode redResourcePoint = objectMapper.createObjectNode();
        redResourcePoint.put("id", "neutral_resource_point_1");
        redResourcePoint.put("type", "resource_point");
        redResourcePoint.put("amount", 3000);
        redResourcePoint.put("x", 25);
        redResourcePoint.put("y", 40);
        neutralBuildings.add(redResourcePoint);
        
        // 蓝色阵营资源点
        ObjectNode blueResourcePoint = objectMapper.createObjectNode();
        blueResourcePoint.put("id", "neutral_resource_point_2");
        blueResourcePoint.put("type", "resource_point");
        blueResourcePoint.put("amount", 3000);
        blueResourcePoint.put("x", 103);
        blueResourcePoint.put("y", 88);
        neutralBuildings.add(blueResourcePoint);
        
        // 中立资源点1
        ObjectNode neutralResource1 = objectMapper.createObjectNode();
        neutralResource1.put("id", "neutral_resource_1");
        neutralResource1.put("type", "resource_point");
        neutralResource1.put("amount", 5000);
        neutralResource1.put("x", 64);
        neutralResource1.put("y", 50);
        neutralBuildings.add(neutralResource1);
        
        // 中立资源点2
        ObjectNode neutralResource2 = objectMapper.createObjectNode();
        neutralResource2.put("id", "neutral_resource_2");
        neutralResource2.put("type", "resource_point");
        neutralResource2.put("amount", 5000);
        neutralResource2.put("x", 64);
        neutralResource2.put("y", 78);
        neutralBuildings.add(neutralResource2);
        
        neutralResources.set("buildings", neutralBuildings);
        initialState.set("neutral", neutralResources);
        
        return initialState;
    }

    /**
     * 创建初始游戏状态 (适用于多人游戏，地图大小为256*256)
     * @return 包含所有阵营初始状态的ObjectNode
     */
    private ObjectNode createInitialGameState() {
        ObjectNode initialState = objectMapper.createObjectNode();
        
        // 为每个阵营创建初始单位
        for (Player player : room.getPlayers()) {
            int campId = player.getCamp().getId();
            Camp camp = player.getCamp();
            ObjectNode campState = objectMapper.createObjectNode();
            
            // 创建建筑物数组
            ArrayNode buildings = objectMapper.createArrayNode();
            
            // 添加主要基地 (没有坦克工厂)
            ObjectNode base = objectMapper.createObjectNode();
            base.put("id", "base_" + campId);
            base.put("type", "base");
            
            // 根据阵营设置不同的初始位置 (在256*256场景中)
            if (camp == Camp.Red) { // 红色阵营 - 左上角
                base.put("x", 32);
                base.put("y", 32);
            } else if (camp == Camp.Blue) { // 蓝色阵营 - 右下角
                base.put("x", 224);
                base.put("y", 224);
            } else if (camp == Camp.Green) { // 绿色阵营 - 左下角
                base.put("x", 32);
                base.put("y", 224);
            } else if (camp == Camp.Yellow) { // 黄色阵营 - 右上角
                base.put("x", 224);
                base.put("y", 32);
            } else if (camp == Camp.Orange) { // 橙色阵营 - 上方中间
                base.put("x", 128);
                base.put("y", 32);
            } else if (camp == Camp.Purple) { // 紫色阵营 - 下方中间
                base.put("x", 128);
                base.put("y", 224);
            } else if (camp == Camp.Pink) { // 粉色阵营 - 左侧中间
                base.put("x", 32);
                base.put("y", 128);
            } else if (camp == Camp.Brown) { // 棕色阵营 - 右侧中间
                base.put("x", 224);
                base.put("y", 128);
            } else { // 其他阵营默认位置
                base.put("x", 128);
                base.put("y", 128);
            }
            
            buildings.add(base);
            
            campState.set("buildings", buildings);
            
            // 创建单位数组
            ArrayNode units = objectMapper.createArrayNode();
            
            // 添加5辆坦克
            for (int i = 1; i <= 5; i++) {
                ObjectNode tank = objectMapper.createObjectNode();
                tank.put("id", "tank_" + campId + "_" + i);
                tank.put("type", "tank");
                
                // 设置坦克位置，围绕基地分布 (在256*256场景中)
                if (camp == Camp.Red) { // 红色阵营
                    switch (i) {
                        case 1: 
                            tank.put("x", 20);
                            tank.put("y", 32);
                            break;
                        case 2:
                            tank.put("x", 32);
                            tank.put("y", 20);
                            break;
                        case 3:
                            tank.put("x", 44);
                            tank.put("y", 32);
                            break;
                        case 4:
                            tank.put("x", 32);
                            tank.put("y", 44);
                            break;
                        case 5:
                            tank.put("x", 25);
                            tank.put("y", 25);
                            break;
                    }
                } else if (camp == Camp.Blue) { // 蓝色阵营
                    switch (i) {
                        case 1:
                            tank.put("x", 212);
                            tank.put("y", 224);
                            break;
                        case 2:
                            tank.put("x", 224);
                            tank.put("y", 236);
                            break;
                        case 3:
                            tank.put("x", 236);
                            tank.put("y", 224);
                            break;
                        case 4:
                            tank.put("x", 224);
                            tank.put("y", 212);
                            break;
                        case 5:
                            tank.put("x", 231);
                            tank.put("y", 231);
                            break;
                    }
                } else if (camp == Camp.Green) { // 绿色阵营
                    switch (i) {
                        case 1:
                            tank.put("x", 20);
                            tank.put("y", 224);
                            break;
                        case 2:
                            tank.put("x", 32);
                            tank.put("y", 236);
                            break;
                        case 3:
                            tank.put("x", 44);
                            tank.put("y", 224);
                            break;
                        case 4:
                            tank.put("x", 32);
                            tank.put("y", 212);
                            break;
                        case 5:
                            tank.put("x", 25);
                            tank.put("y", 231);
                            break;
                    }
                } else if (camp == Camp.Yellow) { // 黄色阵营
                    switch (i) {
                        case 1:
                            tank.put("x", 212);
                            tank.put("y", 32);
                            break;
                        case 2:
                            tank.put("x", 224);
                            tank.put("y", 44);
                            break;
                        case 3:
                            tank.put("x", 236);
                            tank.put("y", 32);
                            break;
                        case 4:
                            tank.put("x", 224);
                            tank.put("y", 20);
                            break;
                        case 5:
                            tank.put("x", 231);
                            tank.put("y", 25);
                            break;
                    }
                } else if (camp == Camp.Orange) { // 橙色阵营
                    switch (i) {
                        case 1:
                            tank.put("x", 116);
                            tank.put("y", 32);
                            break;
                        case 2:
                            tank.put("x", 128);
                            tank.put("y", 20);
                            break;
                        case 3:
                            tank.put("x", 140);
                            tank.put("y", 32);
                            break;
                        case 4:
                            tank.put("x", 128);
                            tank.put("y", 44);
                            break;
                        case 5:
                            tank.put("x", 121);
                            tank.put("y", 25);
                            break;
                    }
                } else if (camp == Camp.Purple) { // 紫色阵营
                    switch (i) {
                        case 1:
                            tank.put("x", 116);
                            tank.put("y", 224);
                            break;
                        case 2:
                            tank.put("x", 128);
                            tank.put("y", 236);
                            break;
                        case 3:
                            tank.put("x", 140);
                            tank.put("y", 224);
                            break;
                        case 4:
                            tank.put("x", 128);
                            tank.put("y", 212);
                            break;
                        case 5:
                            tank.put("x", 135);
                            tank.put("y", 231);
                            break;
                    }
                } else if (camp == Camp.Pink) { // 粉色阵营
                    switch (i) {
                        case 1:
                            tank.put("x", 32);
                            tank.put("y", 116);
                            break;
                        case 2:
                            tank.put("x", 20);
                            tank.put("y", 128);
                            break;
                        case 3:
                            tank.put("x", 32);
                            tank.put("y", 140);
                            break;
                        case 4:
                            tank.put("x", 44);
                            tank.put("y", 128);
                            break;
                        case 5:
                            tank.put("x", 25);
                            tank.put("y", 135);
                            break;
                    }
                } else if (camp == Camp.Brown) { // 棕色阵营
                    switch (i) {
                        case 1:
                            tank.put("x", 224);
                            tank.put("y", 116);
                            break;
                        case 2:
                            tank.put("x", 236);
                            tank.put("y", 128);
                            break;
                        case 3:
                            tank.put("x", 224);
                            tank.put("y", 140);
                            break;
                        case 4:
                            tank.put("x", 212);
                            tank.put("y", 128);
                            break;
                        case 5:
                            tank.put("x", 231);
                            tank.put("y", 121);
                            break;
                    }
                } else { // 其他阵营
                    tank.put("x", 120 + i * 2);
                    tank.put("y", 128);
                }
                
                units.add(tank);
            }
            
            // 添加初始资金2200
            campState.put("money", 2200);
            
            campState.set("units", units);
            initialState.set(String.valueOf(campId), campState);
        }
        
        // 添加中央中立资源点
        ObjectNode neutralResources = objectMapper.createObjectNode();
        ArrayNode neutralBuildings = objectMapper.createArrayNode();
        
        // 中立资源点1
        ObjectNode neutralResource1 = objectMapper.createObjectNode();
        neutralResource1.put("id", "neutral_resource_1");
        neutralResource1.put("type", "resource_point");
        neutralResource1.put("amount", 5000);
        neutralResource1.put("x", 128);
        neutralResource1.put("y", 100);
        neutralBuildings.add(neutralResource1);
        
        // 中立资源点2
        ObjectNode neutralResource2 = objectMapper.createObjectNode();
        neutralResource2.put("id", "neutral_resource_2");
        neutralResource2.put("type", "resource_point");
        neutralResource2.put("amount", 5000);
        neutralResource2.put("x", 128);
        neutralResource2.put("y", 156);
        neutralBuildings.add(neutralResource2);
        
        neutralResources.set("buildings", neutralBuildings);
        initialState.set("neutral", neutralResources);
        
        return initialState;
    }
}
