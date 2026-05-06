package org.game.ra2.util;

import com.fasterxml.jackson.databind.ObjectMapper;

public class ObjectMapperProvider {
    private static final ObjectMapper INSTANCE = new ObjectMapper();
    
    public static ObjectMapper getInstance() {
        return INSTANCE;
    }
}