package org.game.ra2.service;

import org.game.ra2.thread.RoomThread;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * RoomService管理器，用于管理多个RoomService实例
 */
public class RoomServiceManager {
    /**
     * 单例实例
     */
    private static final RoomServiceManager instance = new RoomServiceManager();
    /**
     * 房间线程列表
     */
    private final List<RoomThread> roomThreads = new ArrayList<>();
    private final AtomicInteger currentIndex = new AtomicInteger(0);
    /**
     * 房间服务缓存
     */
    private final ConcurrentHashMap<String, RoomService> roomServices = new ConcurrentHashMap<>();
    /**
     * roomId分配器
     */
    private final AtomicInteger roomIdAllocator = new AtomicInteger(0);
    
    private RoomServiceManager() {
        // 初始化多个RoomThread实例
        for (int i = 0; i < 4; i++) {
            RoomThread roomThread = new RoomThread("RoomThread-" + i);
            roomThreads.add(roomThread);
            roomThread.start();
        }
    }
    
    public static RoomServiceManager getInstance() {
        return instance;
    }
    
    /**
     * 创建一个新的RoomService并分配给一个RoomThread
     * @return RoomService实例
     */
    public RoomService createRoomService() {
        // 分配房间ID
        int incId = roomIdAllocator.getAndIncrement();
        String roomId = "room_" + incId;
        // 分配一个RoomThread
        int index = currentIndex.getAndIncrement() % roomThreads.size();
        RoomThread roomThread = roomThreads.get(index);
        // 创建RoomService实例
        RoomService roomService = new RoomService(roomId, roomThread);
        roomServices.put(roomId, roomService);
        roomThread.addRoomService(roomService);
        return roomService;
    }
    
    /**
     * 根据房间ID获取RoomService
     * @param roomId 房间ID
     * @return RoomService实例
     */
    public RoomService getRoomService(String roomId) {
        return roomServices.get(roomId);
    }
    
    /**
     * 移除RoomService
     * @param roomId 房间ID
     */
    public void removeRoomService(String roomId) {
        RoomService roomService = roomServices.remove(roomId);
        if (roomService != null) {
            roomService.stopService();
        }
    }
    
    /**
     * 停止所有RoomService
     */
    public void stopAllServices() {
        for (RoomThread roomThread : roomThreads) {
            roomThread.stopRunning();
        }
        roomServices.clear();
        HistoryFrameCacheService.getInstance().shutdownGracefully();
        HistoryFramePersistenceService.getInstance().shutdownGracefully();
    }
}
