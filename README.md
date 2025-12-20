![版本](https://img.shields.io/badge/version-0.0.5-blue)
![平台](https://img.shields.io/badge/platform-Windows-lightgrey)
![授權](https://img.shields.io/badge/license-MIT-green)

一款專為《血染鐘樓》（Blood on the Clocktower）設計的劇本編輯器。

## 📥 下載

前往 [Releases 頁面](https://github.com/joeken45/BloodClockTowerScriptEditor/releases) 下載最新版本。

**最新版本：** [v0.0.5](https://github.com/joeken45/BloodClockTowerScriptEditor/releases/tag/v0.0.5)

## ✨ 功能特色

- ✅ 視覺化角色編輯介面
- ✅ 支援 BOTC 官方 JSON 格式
- ✅ 夜晚順序自動排序
- ✅ 相剋規則管理
- ✅ 角色總表匯入
- ✅ Special 特殊功能編輯
- ✅ 劇本匯入/匯出

## 🖥️ 系統需求

- **作業系統：** Windows 10 或更新版本
- **架構：** 64位元
- **其他：** 不需要額外安裝 .NET Runtime

## 🚀 快速開始

1. 前往 [Releases](https://github.com/joeken45/BloodClockTowerScriptEditor/releases) 下載最新版本
2. 解壓縮 ZIP 檔案到任意資料夾
3. 執行 `BloodClockTowerScriptEditor.exe`
4. 開始編輯你的劇本！

## 📖 使用說明

### 建立新劇本

1. 點擊「檔案」→「開啟新檔」
2. 設定劇本名稱和基本資訊

### 匯入角色總表

1. 準備角色總表 JSON 檔案
2. 在程式中匯入角色總表
3. 從角色庫新增角色到劇本

### 編輯角色

1. 選擇劇本中的角色
2. 在右側面板編輯角色屬性
3. 設定夜晚順序、提示標記等

### 匯出劇本

1. 點擊「檔案」→「儲存檔案」
2. 選擇儲存位置
3. 產生 BOTC 格式 JSON 檔案

## 📸 截圖

（待補充：加入程式截圖）

## 🐛 問題回報

如有問題或建議，請前往 [Issues](https://github.com/joeken45/BloodClockTowerScriptEditor/issues) 回報。

## 📝 更新日誌

### v0.0.1 (2025-11-06)
- 🚀 初版發布
- ✅ 角色編輯功能
- ✅ 劇本匯入/匯出
- ✅ 夜晚順序管理
- ✅ 相剋規則設定

### v0.0.2 (2025-11-08)
- 輸出時皆不輸出 爪牙資訊、惡魔資訊 BOTC格式 不輸出 黎明、黃昏、相剋規則
- 角色順序輸出 Bug修正
- 劇本資訊介面優化
- 角色總表.json 更新 夜晚階段定位 爪牙資訊=2000 惡魔資訊 =3000
- 夜間順序移動優化且重要階段(黎明、黃昏、爪牙資訊、惡魔資訊)不可移動、編輯

### v0.0.3 (2025-11-15)
- 新增奇遇角色類別與相關設置與操作介面
- 修改官方ID
- 角色總表.json 對山雨欲來相關角色加入夜間順序與說明

### v0.0.4 (2025-12-01)
- 新增奇遇角色：異術士
- 角色總表.json 修正角色敘述、標記缺失，新增部分BOTC特殊能力
- 集石格式也不會匯出 黎明、黃昏
- BOTC格式匯出時 相剋規則只會輸出在主要角色

### v0.0.5 (2025-12-20)
- 相剋規則.json、角色總表.json 更新/修正
- 另存新檔時自動賦予劇本名稱
- 角色位置拖曳至邊界會自動滾動

完整更新日誌請參考 [CHANGELOG.md](CHANGELOG.md)

## 📄 授權

本專案採用 [MIT 授權](LICENSE)。

## 🙏 致謝

- [Blood on the Clocktower](https://bloodontheclocktower.com/) 官方
- [集石](https://clocktower.gstonegames.com/) 官方
- BOTC 中文社群
- 所有測試人員和貢獻者

## 📧 聯絡方式

- GitHub: [joeken45](https://github.com/joeken45)
- Email: charlesyen7971@gmail.com

---

**注意：** 本工具為非官方專案，與 The Pandemonium Institute 無關。
