package org.game.ra2.client;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.game.ra2.util.ObjectMapperProvider;
import org.java_websocket.client.WebSocketClient;
import org.java_websocket.handshake.ServerHandshake;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

import java.net.URI;
import java.util.Random;
import java.util.Scanner;

public class TestClient extends WebSocketClient {
    private static final Logger logger = LogManager.getLogger(TestClient.class);
    private static final ObjectMapper objectMapper = ObjectMapperProvider.getInstance();
    private final String clientId;
    private final String roomType;
    private boolean matched = false;
    private boolean gameStarted = false;
    private int frameCount = 0;
    private final Random random = new Random();

    public TestClient(URI serverUri, String clientId, String roomType) {
        super(serverUri);
        this.clientId = clientId;
        this.roomType = roomType != null ? roomType.toUpperCase() : "DUO"; // 默认为双人房
    }

    @Override
    public void onOpen(ServerHandshake handshake) {
        logger.info("[{}] 连接建立成功", clientId);
        // 连接成功后发送匹配请求
        sendMatchRequest();
    }

    @Override
    public void onMessage(String message) {
        try {
            JsonNode jsonNode = objectMapper.readTree(message);
            String type = jsonNode.get("type").asText();

            switch (type) {
                case "matchSuccess":
                    handleMatchSuccess(jsonNode);
                    break;
                case "gameStart":
                    handleGameStart();
                    break;
                case "frameSync":
                    handleFrameSync(jsonNode);
                    break;
                default:
                    logger.warn("[{}] 收到未知消息类型: {}", clientId, type);
            }
        } catch (Exception e) {
            logger.error("处理消息时发生错误", e);
        }
    }

    @Override
    public void onClose(int code, String reason, boolean remote) {
        logger.info("[{}] 连接关闭: {}", clientId, reason);
    }

    @Override
    public void onError(Exception ex) {
        logger.error("客户端发生错误", ex);
    }

    private void sendMatchRequest() {
        try {
            ObjectNode request = objectMapper.createObjectNode();
            request.put("type", "match");
            
            ObjectNode data = objectMapper.createObjectNode();
            data.put("name", clientId);
            data.put("roomType", roomType); // 添加房间类型参数
            
            request.set("data", data);
            
            String message = objectMapper.writeValueAsString(request);
            send(message);
            logger.info("[{}] 发送匹配请求: {}", clientId, message);
        } catch (Exception e) {
            logger.error("发送匹配请求时发生错误", e);
        }
    }

    private void handleMatchSuccess(JsonNode message) {
        matched = true;
        logger.info("[{}] 匹配成功.message:{}", clientId, message);
        // 打印房间ID和你的campId和token
        String roomId = message.get("roomId").asText();
        int campId = message.get("yourCampId").asInt();
        String token = message.get("yourToken").asText();
        JsonNode data = message.get("data");
        logger.info("[{}] 房间ID: {}, CampID: {}, Token: {}", clientId, roomId, campId, token);

        // 发送准备就绪消息
        try {
            ObjectNode request = objectMapper.createObjectNode();
            request.put("type", "ready");
            String readyMessage = objectMapper.writeValueAsString(request);
            send(readyMessage);
            logger.info("[{}] 发送准备就绪", clientId);
        } catch (Exception e) {
            logger.error("发送准备就绪消息时发生错误", e);
        }
    }

    private void handleGameStart() {
        gameStarted = true;
        logger.info("[{}] 游戏开始", clientId);
    }

    private void handleFrameSync(JsonNode message) {
        int frame = message.get("frame").asInt();
        logger.info("[{}] 收到帧同步数据 - 帧号: {}, 数据: {}", clientId, frame, message.toString());
        
        // 模拟发送下一帧的输入数据
//        sendFrameInput(frame + 2);
    }

    private void sendFrameInput(int frame) {
        try {
            ObjectNode request = objectMapper.createObjectNode();
            request.put("type", "frameInput");
            request.put("frame", frame);
            
            ArrayNode inputs = objectMapper.createArrayNode();
            ObjectNode input = objectMapper.createObjectNode();
            input.put("id", clientId);
            
            ArrayNode inputData = objectMapper.createArrayNode();
            // 添加一些随机输入数据
            for (int i = 0; i < 3; i++) {
                inputData.add(random.nextInt(10));
            }
            
            input.set("input", inputData);
            inputs.add(input);
            
            request.set("inputs", inputs);
            
            String message = objectMapper.writeValueAsString(request);
            send(message);
            logger.info("[{}] 发送第 {} 帧输入数据", clientId, frame);
        } catch (Exception e) {
            logger.error("发送帧输入数据时发生错误", e);
        }
    }

    public static void main(String[] args) {
        try {
            if (args.length < 1) {
                logger.info("请提供客户端ID作为参数，例如: Player1 [房间类型]");
                logger.info("房间类型: SOLO, DUO, TRIO, QUAD, OCTO");
                return;
            }
            
            String clientId = args[0];
            String roomType = args.length > 1 ? args[1] : "DUO";
            
            // 启动客户端
            TestClient client = new TestClient(new URI("ws://localhost:8080/ws"), clientId, roomType);
            client.connect();
            
            // 等待用户输入退出指令
            Scanner scanner = new Scanner(System.in);
            logger.info("按回车键退出...");
            scanner.nextLine();
            
            client.close();
        } catch (Exception e) {
            logger.error("启动客户端时发生错误", e);
        }
    }
}