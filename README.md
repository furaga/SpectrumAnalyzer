SpectrumAnalyzer
===========================

スペクトラムアナライザのシンプルな実装。

![screenshot](http://furaga.sakura.ne.jp/publish/SpectrumAnalyzer.png)

マイク音源をリアルタイムにフーリエ変換して各周波数成分を色の濃淡で表示します。  
Visual Studio 2010 (C#) で開発しました。  
マイク音源の取得には[NAudio](http://naudio.codeplex.com/)、フーリエ変換には [ILNumerics](http://ilnumerics.net/)を使っています。（NuGetでインストール）  
バイナリは[こちら](http://furaga.sakura.ne.jp/) からダウンロードできます。  

