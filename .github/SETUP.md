# GitHub Actions 构建配置说明

本文档说明如何配置 GitHub Actions 自动构建 Android 和 iOS 包并上传到蒲公英平台。

## 🔧 必需的 GitHub Secrets 配置

在 GitHub 仓库的 `Settings` -> `Secrets and variables` -> `Actions` 中添加以下 secrets：

### 1. Unity 许可证相关

| Secret 名称 | 说明 | 获取方法 |
|------------|------|---------|
| `UNITY_LICENSE` | Unity 许可证内容 | 运行 `unity-editor -quit -batchmode -createManualActivationFile` 然后手动激活 |
| `UNITY_EMAIL` | Unity 账号邮箱 | 您的 Unity 账号邮箱 |
| `UNITY_PASSWORD` | Unity 账号密码 | 您的 Unity 账号密码 |

> **获取 Unity License 的详细步骤:**
> 1. 访问 [Unity Activation](https://license.unity3d.com/manual)
> 2. 本地运行 Unity 生成激活文件
> 3. 上传激活文件获取许可证
> 4. 将许可证内容复制到 `UNITY_LICENSE` secret 中

### 2. Android 签名相关

| Secret 名称 | 说明 | 获取方法 |
|------------|------|---------|
| `ANDROID_KEYSTORE_BASE64` | Keystore 文件的 Base64 编码 | `base64 -i your-keystore.keystore` |
| `ANDROID_KEYSTORE_PASS` | Keystore 密码 | 创建 keystore 时设置的密码 |
| `ANDROID_KEYALIAS_NAME` | Key 别名 | 创建 keystore 时设置的别名 |
| `ANDROID_KEYALIAS_PASS` | Key 密码 | 创建 keystore 时设置的密码 |

> **创建 Android Keystore:**
> ```bash
> keytool -genkey -v -keystore your-keystore.keystore \
>   -alias your-alias -keyalg RSA -keysize 2048 -validity 10000
> 
> # 转换为 Base64
> base64 -i your-keystore.keystore | pbcopy  # macOS
> base64 -w 0 your-keystore.keystore          # Linux
> certutil -encode your-keystore.keystore tmp.b64 && findstr /v /c:- tmp.b64 > keystore.b64  # Windows
> ```

### 3. iOS 签名相关

| Secret 名称 | 说明 | 获取方法 |
|------------|------|---------|
| `IOS_CERTIFICATE_BASE64` | iOS 开发证书的 Base64 编码 | 从钥匙串导出 .p12 文件后编码 |
| `IOS_CERTIFICATE_PASSWORD` | 证书密码 | 导出 .p12 时设置的密码 |
| `IOS_PROVISION_PROFILE_BASE64` | Provisioning Profile 的 Base64 编码 | 从 Apple Developer 下载后编码 |
| `IOS_PROVISION_PROFILE_NAME` | Provisioning Profile 名称 | 在 Apple Developer 中查看 |
| `IOS_TEAM_ID` | Apple Team ID | 在 Apple Developer 账号中查看 |
| `IOS_BUNDLE_ID` | App Bundle ID | 您的应用 Bundle Identifier |
| `KEYCHAIN_PASSWORD` | 临时 Keychain 密码 | 随机生成一个强密码即可 |

> **导出 iOS 证书和 Provisioning Profile:**
> 
> **证书 (.p12):**
> 1. 打开 macOS 钥匙串访问
> 2. 找到您的 iOS 开发/发布证书
> 3. 右键 -> 导出 -> 保存为 .p12 格式
> 4. 转换为 Base64: `base64 -i certificate.p12 | pbcopy`
> 
> **Provisioning Profile:**
> 1. 登录 [Apple Developer](https://developer.apple.com/)
> 2. Certificates, Identifiers & Profiles -> Profiles
> 3. 下载 Ad Hoc 或 Development Profile
> 4. 转换为 Base64: `base64 -i profile.mobileprovision | pbcopy`

### 4. 蒲公英相关

| Secret 名称 | 说明 | 获取方法 |
|------------|------|---------|
| `PGYER_API_KEY` | 蒲公英 API Key | 登录蒲公英 -> 账号设置 -> API 信息 |
| `PGYER_BUILD_PASSWORD` | 应用安装密码（可选） | 自定义设置，用户下载时需要输入 |

> **获取蒲公英 API Key:**
> 1. 登录 [蒲公英](https://www.pgyer.com/)
> 2. 点击右上角头像 -> 账号设置
> 3. 找到 "API 信息" 部分
> 4. 复制 `API Key` 到 GitHub Secrets

## 📋 快速配置检查清单

- [ ] Unity 许可证已配置（3个 secrets）
- [ ] Android 签名已配置（4个 secrets）
- [ ] iOS 签名已配置（7个 secrets）
- [ ] 蒲公英 API 已配置（2个 secrets）

## 💰 成本优化说明

### iOS 构建优化策略

本项目的 iOS 构建采用了**两阶段构建**策略，大幅降低 CI/CD 成本：

| 阶段 | 运行环境 | 耗时 | 相对成本 | 任务 |
|------|---------|------|---------|------|
| 第一阶段 | Linux (ubuntu-latest) | ~15-25分钟 | 1x | Unity 导出 Xcode 项目 |
| 第二阶段 | macOS (macos-latest) | ~5-10分钟 | 10x | 编译和签名 IPA |

**成本节省对比：**
- ❌ **原方案**: 全部在 macOS 上构建 (~30分钟) = 300分钟成本
- ✅ **优化方案**: Linux (20分钟) + macOS (8分钟) = 20 + 80 = 100分钟成本
- 💡 **节省约 67% 的 CI/CD 成本！**

### Android 构建
Android 全程在 Linux 上构建，成本已经是最优的。

## 🚀 触发构建

本项目配置为**仅手动触发**构建，不会自动构建。

### 手动触发步骤
1. 进入 GitHub 仓库的 `Actions` 标签
2. 在左侧选择要执行的 workflow：
   - `Build Android and Upload to Pgyer` - 构建 Android APK
   - `Build iOS and Upload to Pgyer` - 构建 iOS IPA
3. 点击右侧的 `Run workflow` 按钮
4. 可选填写 **版本名称**（会显示在蒲公英的更新说明中）
5. 点击绿色的 `Run workflow` 按钮开始构建

### 查看构建进度
- 在 Actions 页面可以看到构建状态
- 点击具体的 workflow run 查看详细日志
- 构建完成后，在日志中可以找到蒲公英下载链接和安装密码

## 📱 安装应用

构建完成后，在 Actions 日志中会看到：
```
✅ 上传成功!
📱 下载链接: https://www.pgyer.com/xxxxx
🔑 安装密码: your-password
```

访问下载链接，输入密码即可安装应用。

## 🔧 iOS 两阶段构建工作原理

### 为什么可以这样优化？

1. **Unity 是跨平台的**：Unity 可以在 Linux 上导出 iOS 的 Xcode 项目，不需要 macOS
2. **只有签名需要 macOS**：只有最后的编译、签名和打包步骤必须在 macOS 上进行
3. **Artifacts 传递**：GitHub Actions 可以在不同 job 之间传递文件

### 构建流程详解

```
┌─────────────────────────────────────────────────────────┐
│ Job 1: export-xcode-project (ubuntu-latest, 便宜)        │
├─────────────────────────────────────────────────────────┤
│ 1. 检出代码                                              │
│ 2. 缓存 Unity Library                                   │
│ 3. Unity 导出 Xcode 项目 (build/iOS/)                   │
│ 4. 上传 Xcode 项目到 Artifacts                          │
└─────────────────────────────────────────────────────────┘
                        ↓ (传递 Xcode 项目)
┌─────────────────────────────────────────────────────────┐
│ Job 2: build-ipa (macos-latest, 昂贵但必需)              │
├─────────────────────────────────────────────────────────┤
│ 1. 下载 Xcode 项目                                      │
│ 2. 配置签名证书和 Provisioning Profile                  │
│ 3. 使用 xcodebuild 编译和导出 IPA                       │
│ 4. 上传到蒲公英                                         │
└─────────────────────────────────────────────────────────┘
```

## ⚠️ 注意事项

1. **Unity 版本**: 确保 workflow 中的 Unity 版本与项目版本一致，可以在 workflow 文件中添加：
   ```yaml
   with:
     unityVersion: 2021.3.x  # 修改为您的 Unity 版本
   ```

2. **构建时间**: 
   - **Android**: 15-30 分钟（全程 Linux）
   - **iOS**: 25-35 分钟（Linux 20分钟 + macOS 8分钟）
   - **传统 iOS 方式**: 30-45 分钟（全程 macOS，成本高 3倍）

3. **免费额度**: GitHub Actions 对私有仓库有使用时间限制，请注意配额
   - 免费版: 2000分钟/月
   - macOS 分钟数按 10x 计算
   - 采用优化方案后，每次 iOS 构建仅消耗约 100 等效分钟而非 300

4. **iOS 两阶段构建**: 
   - Unity 可以在 Linux 上导出 Xcode 项目
   - 只有最后的编译和签名需要 macOS
   - 通过 Artifacts 在两个 job 之间传递文件

5. **蒲公英上传限制**: 
   - 免费用户每日下载次数有限制
   - 应用大小限制为 2GB
   - 需要完成实名认证才能正常使用

## 🔍 故障排查

### Unity 许可证问题
如果出现许可证错误，检查：
- `UNITY_LICENSE` 是否完整复制
- Unity 版本是否匹配
- 许可证是否过期

### Android 构建失败
检查：
- Keystore 信息是否正确
- Base64 编码是否完整
- 密码是否正确

### iOS 构建失败

**第一阶段（导出 Xcode 项目）失败：**
- Unity 许可证是否正确
- Unity 版本是否匹配
- 项目是否有编译错误

**第二阶段（编译 IPA）失败：**
- 证书是否在有效期内
- Provisioning Profile 是否匹配 Bundle ID
- Team ID 是否正确
- UDID 是否已添加到 Provisioning Profile
- Xcode 项目是否成功从第一阶段传递过来

### 蒲公英上传失败
检查：
- API Key 是否正确
- 网络连接是否正常
- 应用文件是否生成成功
- 是否已完成实名认证

### Artifacts 传递问题
如果 iOS 第二阶段找不到 Xcode 项目：
- 检查第一阶段是否成功完成
- 查看 Artifacts 是否成功上传
- 确认 `retention-days` 设置（默认1天）

## 📚 相关文档

- [Unity CI/CD Documentation](https://game.ci/docs)
- [蒲公英 API 文档](https://www.pgyer.com/doc/view/api)
- [GitHub Actions 文档](https://docs.github.com/actions)

