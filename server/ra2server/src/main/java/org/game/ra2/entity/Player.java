package org.game.ra2.entity;

import org.apache.commons.lang3.RandomUtils;
import org.apache.commons.lang3.builder.ToStringBuilder;
import org.apache.commons.lang3.math.NumberUtils;

/**
 * 玩家实体类
 */
public class Player {
    /**
     * 阵营ID
     */
    private final Camp camp;
    /**
     * 频道ID
     */
    private String channelId;
    /**
     * 玩家名称
     */
    private String name;
    /**
     * 是否有效
     */
    private boolean channelValid;
    /**
     * token
     */
    private String token;

    public Player(Camp camp) {
        this.camp = camp;
        this.channelValid = true;
        // 随机token：事件戳+随机数
        this.token = String.valueOf(RandomUtils.nextLong(100000000L, 999999999L));
    }

    public Camp getCamp() {
        return camp;
    }

    public String getChannelId() {
        return channelId;
    }

    public void setChannelId(String channelId) {
        this.channelId = channelId;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getName() {
        return name;
    }

    public boolean isChannelValid() {
        return channelValid;
    }

    public void setChannelValid(boolean channelValid) {
        this.channelValid = channelValid;
    }

    public String getToken() {
        return token;
    }

    @Override
    public String toString() {
        return new ToStringBuilder(this)
                .append("campId", camp)
                .append("channelId", channelId)
                .append("name", name)
                .toString();
    }
}