package org.game.ra2.netty;

import io.netty.bootstrap.ServerBootstrap;
import io.netty.channel.ChannelFuture;
import io.netty.channel.EventLoopGroup;
import io.netty.channel.nio.NioEventLoopGroup;
import io.netty.channel.socket.nio.NioServerSocketChannel;
import org.game.ra2.service.MatchService;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

/**
 * WebSocket服务器主类
 */
public class WebSocketServer {
    private static final Logger logger = LogManager.getLogger(WebSocketServer.class);

    private final int port;
    private final MatchService matchService;

    public WebSocketServer(int port, MatchService matchService) {
        this.port = port;
        this.matchService = matchService;
    }

    public void start() throws InterruptedException {
        EventLoopGroup bossGroup = new NioEventLoopGroup();
        EventLoopGroup workerGroup = new NioEventLoopGroup();

        try {
            ServerBootstrap bootstrap = new ServerBootstrap();
            bootstrap.group(bossGroup, workerGroup)
                    .channel(NioServerSocketChannel.class)
                    .childHandler(new WebSocketServerInitializer(matchService));

            ChannelFuture future = bootstrap.bind(port).sync();
            logger.info("WebSocket 服务器启动成功，端口：{}", port);
            future.channel().closeFuture().sync();
        } finally {
            bossGroup.shutdownGracefully();
            workerGroup.shutdownGracefully();
        }
    }
}