package org.game.ra2.service;

import com.fasterxml.jackson.databind.JsonNode;
import org.apache.commons.lang3.builder.ToStringBuilder;

/**
 * 匹配消息类
 */
public class Message {
    private final String channelId;
    private final JsonNode data;

    public Message(String channelId, JsonNode data) {
        this.channelId = channelId;
        this.data = data;
    }

    public String getChannelId() {
        return channelId;
    }

    public JsonNode getData() {
        return data;
    }

    @Override
    public String toString() {
        return new ToStringBuilder(this)
                .append("channelId", channelId)
                .append("data", data)
                .toString();
    }
}