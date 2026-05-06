package org.game.ra2;

import org.game.ra2.netty.WebSocketServer;
import org.game.ra2.service.HistoryFrameCacheService;
import org.game.ra2.service.HistoryFramePersistenceService;
import org.game.ra2.service.MatchService;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

/**
 * 游戏启动类
 */
public class GameStartUp {
    private static final Logger logger = LogManager.getLogger(GameStartUp.class);

    public static void main(String[] args) {
        try {
            Runtime.getRuntime().addShutdownHook(new Thread(() -> {
                logger.info("服务器关闭，开始关闭历史帧缓存读取服务和历史帧持久化服务");
                HistoryFrameCacheService.getInstance().shutdownGracefully();
                HistoryFramePersistenceService.getInstance().shutdownGracefully();
            }, "Game-Shutdown-HistoryFrame"));

            // 初始化匹配服务
            MatchService matchService = MatchService.getInstance();
            
            // 启动WebSocket服务器
            WebSocketServer server = new WebSocketServer(8080, matchService);
            server.start();

            logger.info("服务器启动成功，请访问 http://localhost:8080");
            
        } catch (Exception e) {
            logger.error("服务器启动失败", e);
        }
    }
}
