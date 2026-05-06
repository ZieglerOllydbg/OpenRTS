package org.game.ra2.client;

import java.net.URI;
import java.util.ArrayList;
import java.util.List;
import java.util.Scanner;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

public class MultiPlayerTest {
    private static final Logger logger = LogManager.getLogger(MultiPlayerTest.class);
    
    public static void main(String[] args) {
        try {
            logger.info("请选择要测试的房间类型:");
            logger.info("1. SOLO (1人)");
            logger.info("2. DUO (2人)");
            logger.info("3. TRIO (3人)");
            logger.info("4. QUAD (4人)");
            logger.info("5. OCTO (8人)");
            
            Scanner scanner = new Scanner(System.in);
            int choice = scanner.nextInt();
            scanner.nextLine(); // 消费换行符
            
            String roomType;
            int playerCount;
            
            switch (choice) {
                case 1:
                    roomType = "SOLO";
                    playerCount = 1;
                    break;
                case 2:
                    roomType = "DUO";
                    playerCount = 2;
                    break;
                case 3:
                    roomType = "TRIO";
                    playerCount = 3;
                    break;
                case 4:
                    roomType = "QUAD";
                    playerCount = 4;
                    break;
                case 5:
                    roomType = "OCTO";
                    playerCount = 8;
                    break;
                default:
                    logger.warn("无效选择，默认使用DUO模式");
                    roomType = "DUO";
                    playerCount = 2;
                    break;
            }
            
            logger.info("启动 {} 个玩家进行 {} 模式测试", playerCount, roomType);
            
            List<TestClient> clients = new ArrayList<>();
            
            // 创建并连接客户端
            for (int i = 1; i <= playerCount; i++) {
                String clientId = "Player" + i;
                TestClient client = new TestClient(new URI("ws://localhost:8080/ws"), clientId, roomType);
                clients.add(client);
                client.connect();
                
                // 稍微延迟一下，避免连接过于集中
                Thread.sleep(500);
            }
            
            logger.info("所有客户端已启动，按回车键停止所有客户端...");
            scanner.nextLine();
            
            // 关闭所有客户端
            for (TestClient client : clients) {
                client.close();
            }
            
            logger.info("所有客户端已关闭");
            
        } catch (Exception e) {
            logger.error("多玩家测试时发生错误", e);
        }
    }
}