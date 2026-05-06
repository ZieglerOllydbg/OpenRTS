# 启动

## 下载jdk11
https://docs.aws.amazon.com/corretto/latest/corretto-11-ug/downloads-list.html

![](jdk.png)

## 1、解压jdk11压缩包到当前目录：jdk11.0.29_7

解压后，目录结构：
```
server/jdk11.0.29_7
                    /bin
                    /conf
                    ...
```

## 2、运行: jar/ra2.bat
```
Starting ra2 server...
Using JDK: ..\jdk11.0.29_7
2025-10-24 10:45:12
2025-10-24 10:45:13
2025-10-24 10:45:14
2025-10-24 10:45:15
2025-10-24 10:45:16
2025-10-24 10:45:17
2025-10-24 10:45:18
2025-10-24 10:45:19
2025-10-24 10:45:20
```

# 源码目录
ra2server


# linux配置jdk

## 1. 下载 Corretto 11

前往 [Amazon Corretto 下载页面](https://aws.amazon.com/corretto/) 或直接下载：

```
curl -LO https://corretto.aws/downloads/latest/amazon-corretto-11-x64-linux-jdk.tar.gz
```

## 2. 解压到指定目录
```bash
创建安装目录（可选，推荐）
sudo mkdir -p /opt/corretto
解压
sudo tar -xzf amazon-corretto-11-x64-linux-jdk.tar -C /opt/corretto --strip-components 1
```

## 3. 配置环境变量

编辑 ~/.bashrc 或 /etc/profile（系统级）：
```bash
sudo nano ~/.bashrc
```
在文件末尾添加：
```bash
export JAVA_HOME=/opt/corretto
export PATH=$JAVA_HOME/bin:$PATH
```
保存后，应用更改：

```bash
source ~/.bashrc
```
## 4. 验证安装

无论使用哪种方法，安装完成后运行以下命令验证：

```bash
java -version
javac -version
```
输出应类似：
```
openjdk version "11.0.xx" 2023-xx-xx LTS
OpenJDK Runtime Environment Corretto-11.0.xx.xx (build 11.0.xx+xx-LTS)
OpenJDK 64-Bit Server VM Corretto-11.0.xx.xx (build 11.0.xx+xx-LTS, mixed mode)
```

## 5. 卸载 Corretto
- 包管理器安装：使用对应的卸载命令，如 sudo apt remove java-11-amazon-corretto-jdk
- 手动安装：直接删除解压的目录，如 sudo rm -rf /opt/corretto，并移除环境变量。

