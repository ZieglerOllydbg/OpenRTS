package org.game.ra2.service;

import io.netty.channel.Channel;
import io.netty.channel.ChannelFutureListener;
import io.netty.handler.codec.http.websocketx.TextWebSocketFrame;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

import java.util.concurrent.ConcurrentHashMap;

/**
 * WebSocket会话管理器
 */
public class WebSocketSessionManager {
    private static final Logger logger = LogManager.getLogger(WebSocketSessionManager.class);
    private static WebSocketSessionManager instance = new WebSocketSessionManager();
    private final ConcurrentHashMap<String, Channel> channels = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<String, String> channelRoomMap = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<String, Long> channelLastPingTimeMap = new ConcurrentHashMap<>();

    private WebSocketSessionManager() {
    }

    public static WebSocketSessionManager getInstance() {
        return instance;
    }

    public void addChannel(Channel channel) {
        String channelId = channel.id().asLongText();
        channels.put(channelId, channel);
        updatePingTime(channelId);
    }

    public void removeChannel(String channelId) {
        channels.remove(channelId);
        channelRoomMap.remove(channelId);
        channelLastPingTimeMap.remove(channelId);
    }

    public Channel getChannel(String channelId) {
        return channels.get(channelId);
    }

    /**
     * 更新频道最后一次ping时间
     * @param channelId 频道ID
     */
    public void updatePingTime(String channelId) {
        channelLastPingTimeMap.put(channelId, System.currentTimeMillis());
    }

    /**
     * 检查频道ping是否超时
     * @param channelId 频道ID
     * @param timeoutMillis 超时时间，单位毫秒
     * @return true表示ping已超时
     */
    public boolean isPingTimeout(String channelId, long timeoutMillis) {
        Long lastPingTime = channelLastPingTimeMap.get(channelId);
        if (lastPingTime == null) {
            return false;
        }

        return System.currentTimeMillis() - lastPingTime > timeoutMillis;
    }

    /**
     * 主动关闭频道连接
     * @param channelId 频道ID
     */
    public void closeChannel(String channelId) {
        Channel channel = channels.get(channelId);
        if (channel != null && channel.isActive()) {
            channel.close().addListener((ChannelFutureListener) future -> {
                if (!future.isSuccess()) {
                    logger.error("关闭频道失败 - 频道ID: {}", channelId, future.cause());
                }
            });
        }
    }

    /**
     * 线程安全的消息发送方法
     * @param channelId
     * @param message
     */
    public void sendMessage(String channelId, String message) {
        Channel channel = channels.get(channelId);
        if (channel != null && channel.isActive()) {
            channel.eventLoop().execute(() -> {
                channel.writeAndFlush(new TextWebSocketFrame(message)).addListener((ChannelFutureListener) future -> {
                    if (!future.isSuccess()) {
                        logger.error("消息发送失败 - 频道ID: {}, 消息: {}", channelId, message, future.cause());
                    }
                });
            });
        } else {
            logger.warn("无法发送消息到频道: {}, 频道状态: {}", channelId, (channel != null ? "活跃=" + channel.isActive() : "不存在"));
        }
    }
    
    /**
     * 设置频道与房间的映射关系
     * @param channelId 频道ID
     * @param roomId 房间ID
     */
    public void setChannelRoomMapping(String channelId, String roomId) {
        channelRoomMap.put(channelId, roomId);
    }
    
    /**
     * 获取频道所属的房间ID
     * @param channelId 频道ID
     * @return 房间ID
     */
    public String getRoomIdByChannel(String channelId) {
        return channelRoomMap.get(channelId);
    }

    /**
     * channel存在房间
     */
    public boolean isChannelInRoom(String channelId) {
        return channelRoomMap.containsKey(channelId);
    }

    /**
     * 移除频道与房间的映射关系
     * @param channelId 频道ID
     */
    public void removeChannelRoomMapping(String channelId) {
        channelRoomMap.remove(channelId);
    }
}
