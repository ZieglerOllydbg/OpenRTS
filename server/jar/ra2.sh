#!/bin/bash

# 设置应用程序根目录
APP_HOME="$(cd "$(dirname "$0")" && pwd)"

# 设置Java路径
JAVA_HOME="/opt/corretto"
JAVA="$JAVA_HOME/bin/java"

echo "Starting ra2 server..."
echo "Using Java from: $JAVA_HOME"
echo "App home: $APP_HOME"

# 检查Java是否存在
if [ ! -x "$JAVA" ]; then
    echo "Error: Java executable not found at $JAVA"
    echo "Please check your Java installation"
    exit 1
fi

# 运行Java程序
cd "$APP_HOME"
"$JAVA" -Dfile.encoding=UTF-8 -cp "ra2.jar:config/*:libs/*:" org.game.ra2.GameStartUp

# 检查运行结果
if [ $? -ne 0 ]; then
    echo "Failed to start ra2 server."
    exit 1
fi