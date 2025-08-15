# Jirou

[![Unity Version](https://img.shields.io/badge/Unity-6000.2.0f1%20LTS-blue.svg)](https://unity.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20PC-lightgrey.svg)](https://github.com/yourusername/jirou)
[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)](https://github.com/yourusername/jirou)
[![Development Status](https://img.shields.io/badge/Status-Early%20Development-orange.svg)](https://github.com/yourusername/jirou)

## 🎯 操作方法

| キー | レーン | 説明 |
|------|--------|------|
| `D` | レーン1（左端） | 左から1番目のレーン |
| `F` | レーン2 | 左から2番目のレーン |
| `J` | レーン3 | 右から2番目のレーン |
| `K` | レーン4（右端） | 右から1番目のレーン |

## 🛠️ 技術仕様

### 開発環境
- **エンジン**: Unity 6000.2.0f1 (Unity 6.0 LTS)
- **レンダーパイプライン**: Universal Render Pipeline (URP)
- **プラットフォーム**: Windows PC
- **言語**: C#

### 主要Unityパッケージ
- Universal Render Pipeline (URP) 17.2.0
- Input System 1.14.1
- TextMeshPro (UGUI 2.0.0)
- Timeline 1.8.7
- Visual Scripting 1.9.7

### システム要件
- **OS**: Windows 10以降
- **DirectX**: DirectX 11対応
- **メモリ**: 4GB RAM以上推奨
- **グラフィック**: DirectX 11対応GPU

## 🚀 セットアップ

### 1. 前提条件
- Unity Hub をインストール
- Unity 6000.2.0f1 LTS（または2022.3 LTS）をインストール
- Git for Windowsをインストール

### 2. プロジェクトクローン
```bash
git clone https://github.com/yourusername/jirou.git
cd jirou
```

### 3. Unityでプロジェクトを開く
1. Unity Hubを起動
2. 「プロジェクトを開く」をクリック
3. `JirouUnity/` フォルダを選択
4. プロジェクトが開いたら、`Assets/_Jirou/Scenes/SampleScene.unity` を開く

### 4. ビルドと実行
1. Unity エディタで `File > Build Settings`
2. シーンがBuild設定に追加されていることを確認
3. プラットフォームを「PC, Mac & Linux Standalone」に設定
4. 「Build」または「Build and Run」を実行

## 🧪 テスト

プロジェクトにはUnity Test Runnerによる自動テスト環境が構築されています。

### テスト実行
1. Unityエディタで `Window > General > Test Runner` を開く
2. EditModeタブまたはPlayModeタブを選択
3. 「Run All」ボタンをクリック

---

<p align="center">
  <img src="https://img.shields.io/badge/Made%20with-Unity-blue.svg" alt="Made with Unity">
  <img src="https://img.shields.io/badge/AI%20Assisted-Claude%20Code-purple.svg" alt="AI Assisted with Claude Code">
  <img src="https://img.shields.io/badge/Powered%20by-URP-orange.svg" alt="Powered by URP">
</p>