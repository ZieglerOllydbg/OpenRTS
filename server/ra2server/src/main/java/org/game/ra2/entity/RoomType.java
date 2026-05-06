package org.game.ra2.entity;

/**
 * 房间类型枚举
 */
public enum RoomType {
    SOLO(1), 
    DUO(2), 
    TRIO(3), 
    QUAD(4), 
    OCTO(8);

    private final int maxPlayers;

    RoomType(int maxPlayers) {
        this.maxPlayers = maxPlayers;
    }

    public int getMaxPlayers() {
        return maxPlayers;
    }
}