[Unity]テクスチャサイズ削減のためのシンプルなアセットポストプロセッサ
===================================================================

Unity上で主にテクスチャサイズを削減するためのシンプルなアセットポストプロセッサ"__SimpleTextureModifier__"を作りました。  
プロジェクト途中から終盤にかけての容量圧縮の必要性に対して、簡単にかつワークフローへの影響を軽微に抑えつつおこなうため、各テクスチャに適切なラベルを設定するだけで、アセットポストプロセッサがそれを認識してテクスチャフォーマットの変換を行います。  

#画像圧縮の例

下のような256x256pixelサイズの32bits TrueColor(RGBA32bits)画像があります。  
Unityチェッカーのインスペクターのプレビューで見ると市松模様の部分がアルファで抜かれた領域です。  

![Original](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A.png)

しかしこのようなRGBA32bits画像は容量が大きくなり、特に実行時のVRAM消費量が大きくなってしまいます(今回のような256x256pixelの画像で1MByteの領域を必要とします)。  

##単純な16bitsフォーマットへの減色

まずは容量は半減させるためにRGBA16bitsフォーマットへ減色をおこなってみます。  

![16bits](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A%2016bits.png)

この例ではグラデーションにマッハバンドが見られます。また肌などの色味が若干変化して見えます、、これはUnityの減色があまり適切ではないアルゴリズムでおこなわれているためです。  

そこで画像にFloyd–Steinbergというディザリングをおこないながら16bitsフォーマットへ減色をおこなってみます。  

![16bitsDither](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A%2016bits%20FloydSteinberg.png)

ディザリングによってザラつき感はでるもののマッハバンドは軽減しています。またUnity標準のものよりも適切な減色アルゴリズムを使用しているために色味の変化もなくなっています。  
このようにディザリングをした16bitsフォーマットへの変換は、容易に画像容量を半減することが可能です。  

しかし、ディザリングはUIに使われる画像などには適切ではないことがあります。  
ディザリングは元画像にノイズを乗せるためにドットバイドット以上の拡大をおこなうと、ディザのパターンも拡大されてしまって目立つことがあるからです。  
これはUGUIのようなUIシステムでよく使われる9パッチという仕組みで顕著に問題になります。UGUIでは[ImageコンポーネントのSlicedを選択すると使用できる機能で](http://docs.unity3d.com/ja/current/Manual/script-Image.html)、画像9つの部分に分割して拡大縮小の制御をおこなうことによって、サイズ変更に柔軟なUI部品にするものです。この場合、9つのパーツの一つである画像の一部が極端に拡大されるようなことが容易におこるため、ディザリングの問題が顕著化しやすいといえます。  
__SimpleTextureModifier__ではこうした問題に対応するために、16bitsへの減色のみをおこなう機能を用意しました。9パッチ等の拡大表示をおこなう画像は単純に16bits減色、おこなわない画像はディザリングをおこなう16bits減色を選択することが可能です。  

##圧縮テクスチャへの変換

iOSやAndroid、WinowsPhoneに限らず多くのグラフィックプロセッサには圧縮テクスチャフォーマットが用意されています。  
iOSではPVRTC、AndroidではETC、microsfoft系ではDXTが、すべての端末で使用可能な圧縮テクスチャフォーマットとなって居るので、これらへの変換をおこなえば大幅なテクスチャ容量の削減が可能になります。  

`このとき気をつけるが、Unityのテクスチャ_Import Settings_の_Alpha Is Transparency_を決してONにしないことです。  
_Alpha Is Transparency_はプレビューでテクスチャの半透明部分に市松模様を描画してわかりやすくしてくれるという_だけの機能ではなく_、透明部分にAlpha Bleeding処理をおこなうことによってテクスチャの透明部と不透明部のきわにゴミが出るのを防ぐといったこともやっている。このUnityのAlpha Bleeding処理が圧縮アルゴリズムと相性が悪いために、圧縮テクスチャの画質を無意味に悪化させてしまっているためです。  
__SimpleTextureModifier__は、そのため処理時に_Alpha Is Transparency_をOFFにする処理を入れています。`

今回はiOSで使用することを想定して様々な圧縮を行ってみましょう。  
RGBA32bits画像を単純にPVRTCへ圧縮した結果です。  

![PVRTCDefault](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A%20Default.png)

透明部分との境界近辺が特に大きく画像が乱れているのがわかります。  
透明部分の裏側にあるRGBの画像に影響をうけるためです。  
こうした単純なシェーダーで処理される素材画像の場合、アルファチャンネルの値が0のpixelは完全な透明ですから、画面に表示されることはありません。  
表示されないのですから不透明部の圧縮を手助けするような画像を描いてしまってもかまわないはずです。  
そこでPVRTCの圧縮アルゴリズムを考慮した上で、透明部分のRGB値を圧縮に有利なように上書きする処理(Alpha Bleeding)をおこなった画像がこれです。  

![PVRTCBleed](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A%20Bleed%20PVRTC%20RGBA.png)

単純にPVRTC圧縮をおこなった場合よりも、境界近辺の品質が向上しているのがわかります。  
ただRGBA32bits画像と比較すると依然モヤモヤしたものを感じます。  
RGBの画質全体を劣化させているのがアルファチャネルの存在だとすれば、例えばアルファチャネルだけを分離してみたらどうでしょう?  

![PVRTCBleedRGB](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A%20Bleed%20with%20alpha%20PVRTC%20RGB.png)![PVRTCBleedA](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A%20Bleed%20with%20alpha%20PVRTC%20A.png)

左側がRGBチャンネルのみを圧縮した画像、右側がアルファチャンネルの圧縮画像です。  
RGBチャンネルの画像のエッジには圧縮を助けるためBleeding処理によって作られた縁取りが見えます。  
この二つの画像をシェーダーを使って合成して表示ます。  

![PVRTCBleedRGB+A](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A%20Bleed%20with%20alpha%20PVRTC.png)![Original](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A.png)

左側は圧縮画像の合成、右側には比較のために再度RGBA32bitsの元画像をのせました。  
比較してみた結果はどうでしょうか?  
皆さんは、たぶんPC上でこれを見ていると思いますが、仮に_Retina Display_など呼ばれるモバイル端末の微細な画面でこうした映像の差を比較した場合さらに僅差に感じると思われます。  

##圧縮テクスチャのマルチプラットフォーム対応

__SimpleTextureModifier__は__Switch Platform__に対応しているのでプラットフォーム切り替え時にプラットフォームごとに適切なフォーマットを出力します。
この場合iOSにおいて、圧縮テクスチャは  

・_RGBA PVRTC 4bitsフォーマット_  
・_RGB PVRTC 4bits(RGB用)+RGB PVRTC 4bits(アルファ用)_  

の選択が可能です。  
これはPVRTCがアルファチャンネル付の圧縮フォーマットをサポートしているから可能となっています(品質は低いにしろ)。  

しかしAndroidの圧縮テクスチャ形式であるETCはアルファチャンネル付の圧縮フォーマットを持っていませんので  

・_RGB ETC 4bits(RGB用)+RGB ETC 4bits(アルファ用)_  

の一択しか選択できません。  

![ETCBleedRGB+A](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A%20Bleed%20with%20alpha%20ETC.png)![Original](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A.png)

左側はETC圧縮のRGB+A画像の合成、右側には比較のためのRGBA32bits画像です。  

また、MicroSoft系プラットフォームで使える圧縮テクスチャ形式DXTもPVRTC同様アルファチャンネル付の圧縮フォーマットをサポートしているため。  

・_RGBA DXT5 8bitsフォーマット_  
・_RGB DXT1 4bits(RGB用)+RGB DXT1 4bits(アルファ用)_  

の選択が可能です。  

上記、RGBA DXT5 8bitsフォーマットは内部でRGBチャンネルとアルファチャンネルを別々に圧縮して持っているので、RGBテクスチャ+アルファテクスチャの組み合わせと、容量的にも画質的にもほぼ等価です。  

![DXT5BleedRGBA](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A%20Bleed%20DXT5%20RGBA.png)![Original](https://github.com/spacebeagle/SimpleTextureModifier/raw/gh-pages/Images/Test%20A.png)

左側はDXT5圧縮のRGBA画像、右側はRGBA32bits画像です。  

様々なプラットフォーム上で共通にアセットデータを使いまわすには、各プラットフォームの公約数を考えるとかありません。  
幸いにもWindows、Mac等のPC上では比較的万能なDXTを使用できますので、iOSとAndroidで共通の設定を考えるなら__RGB+A__で２枚のテクスチャに分割する方法が__SimpleTextureModifier__のみで処理できる範囲でしょう。  

#SimpleTextureModifierを使用する

基本的にUnityのAssets下のEditorデイレクトリのどれかに、_SimpleTextureModifier.cs_と_SimpleTextureModifierSetting.cs_が存在すれば動作します。  
気をつけなければいけないのは、 
UnityのEdit>Preferences..を選択すると出てくる__Unity Preferencesウインドウ__の__TextureModifier__にある__SimpleTextureModifier Enable__スイッチが__ON__であることです。  
ONでないと圧縮テクスチャの出力など__SimpleTextureModifier__の最終処理がおこなわれません。  
これはAndroidプラットフォーム上でのETC圧縮処理などのきわめて時間がかかる処理をスキップするために存在するスイッチです。  
また処理をおこなうテクスチャアセットの出力フォーマット設定は、ARGB32bit、RGBA32bit、Truecolorなどの32bitsフォーマットにしておいてください。これは__SimpleTextureModifier__の処理の前後に余計な処理が入らないようにするために必要です。

#SimpleTextureModifierの使い方

__SimpleTextureModifier__はテクスチャアセットにラベルを設定することにより、アセットポストプロセッサがそれを見て処理をおこないます。  
テクスチャにはラベルをセットするだけですから、__Apply__ボタンがオンにはなりません、テクスチャのImport Settingのいずれかを操作して__Apply__ボタンをオンにしてからそれを押すか、Projectのテクスチャアセットを選択した状態でマウスの右ボタン(Windowsの場合)を押してReimportを選んで強制的にインポート処理が走るようにしてください。  

ラベルはテクスチャアセット上でマウスの右ボタンを押して、__Texture Util__から内から選択して設定することができます。  
ラベルは３つのカテゴリにわかれています。  
"__Texture Effecter__"、"__Texture Modifier__"、"__Texture Output__"です。  
それぞれ１種類の機能を選択でき、この順番に処理されます。  
__Texture Util__内から設定した場合、他の機能を選択すると元からあった機能のラベルは消去されるようになっています。  
最終的なテクスチャ出力は__Texture Output__によっておこない、出力フォーマットもここで確定します。  
__Texture Modifier__には16bits化減色処理がありますが、これらを指定しても内部的なテクスチャフォーマットは32bitsのままです。  
RGBA各チャンネルの8bitsデータの下位4bitsが0にクリアされるような処理のみがおこなわれています。  
これには重要な意味があります、Unityでは32bitsRGBAか8bitsAlpha、DXT等のフォーマットではないとテクスチャからpixel値を読み出すことができません。最終出力である16bitsRGBA や圧縮テクスチャへの変換は、これ以上処理がおこなわれないという最終段階でおこなわなければならないということになります。  

プルダウンで選択可能な機能は以下のようになっています。  

__Texture Effecter__は  
・Clear Texture Effecter Label  
　_Texture Effecterカテゴリのラベルをすべて消去する_  
・Set Label PremultipliedAlpha  
　_Textureを事前乗算済みアルファ[GitHub][PMA]の処理をおこなう_  
・Set Label AlphaBleed  
　_Textureに圧縮に適したAlphaBleed処理をおこなう_  

__Texture Modifier__は  
・Clear Texture Modifier Label  
　_Texture Modifierカテゴリのラベルをすべて消去する_  
・Set Label FloydSteinberg  
　_TextureをFloydSteinbergによってディザ処理をおこない16bits化する_  
・Set Label Reduced16bits  
　_Textureを減色しRGBA16bits化する_  

__Texture Output__は
・Clear Texture Output Label  
　_Texture Outputカテゴリのラベルをすべて消去する_  
・Set Label Convert 16bits  
　_Textureを減色しRGBA16bitsデータとしてアセット化する_  
・Set Label Convert Compressed  
　_Textureをプラットフォームに適した圧縮データとしてアセット化する_  
・Set Label Convert Compressed no alpha  
　_Textureをプラットフォームに適したアルファ無し圧縮データとしてアセット化する_  
・Set Label Convert Compressed with alpha  
　_Textureをプラットフォームに適したアルファ無し圧縮データとしてアセット化し_  
　_アルファチャネルを圧縮データとしてアセット+Alpha名でアセット化する_  
・Set Label Texture PNG  
　_Textureを元のアセットとは別にPNG形式で作成する_  
・Set Label Texture JPG  
　_Textureを元のアセットとは別にJPG形式で作成する_  

これらを組み合わせることによって様々なテクスチャフォーマットを出力するようになっています。

[PMA]:
[乗算済みアルファとは？ その１:補間アルファの問題点]   (http://blogs.msdn.com/b/ito/archive/2010/07/10/what-is-the-premultilied-alpha-part-1.aspx)  
[乗算済みアルファとは？ その2: コンポジション]   (http://blogs.msdn.com/b/ito/archive/2011/09/01/compositoin-with-the-premultiplied-alpha.aspx)  


#サンプルシーンの見かた

　プロジェクトにはサンプルシーンが付属しています。
　これらはシーンを切り替えればGameビュー上に表示されシーンを実行する必要はありません。
　素材となる画像は"Test A"、"Test B"の２種類があり、それをコピーして様々な名前をつけています。これはラベルをつけて固有の変換処理をおこなうためで、元々はすべて同じpngファイルです。
シーンの処理の処理は以下のようになります

1. 素材画像 16bits  
　_RGBA16bits化して表示_  
2. 素材画像 16bits FloydSteinberg  
　_FloydSteinbergをかけRGBA16bits化して表示_  
3. 素材画像 Bleed  
　_Alpha Bleedingをおこない圧縮テクスチャ化して表示_  
4. 素材画像 Bleed with alpha  
　_Alpha BleedingをおこないRGBとアルファに分離、圧縮テクスチャ化して表示_  
5. 素材画像 Default  
　_元画像を無処理で圧縮テクスチャ化して表示_  
6. 素材画像 PMA  
　_事前乗算済みアルファに変換し圧縮テクスチャ化して表示_  
7. 素材画像 PVRTool  
　_PVRTexTool[GitHub][PVRTexTool]のAlpha Bleedingをおこない圧縮テクスチャ化して表示_  
8. 素材画像 PVRTool with alpha  
　_PVRTexToolのAlpha BleedingをおこないRGBとアルファに分離、圧縮テクスチャ化して表示_  
9. 素材画像 Truecolor  
　_元画像をRGBA32bitのまま表示_  
10. 素材画像 with alpha  
　_元画像をRGBとアルファに分離、圧縮テクスチャ化して表示_  


[PVRTexTool]:
PowerVRの設計元であるImagination Technologies社の提供するテクスチャツール。  
圧縮画質向上の方法としてAlpha BleedingとPremultiplied Alpha(事前乗算済みアルファ)を提供している。  

#今後の課題

__SimpleTextureModifier__はUGUIの__Sprite Packer__を使用したアトラス化に対しては処理をおこなうことはできません。
これはアトラス化を担当するPackerJobがアセットポストプロセッサを介さずにアトラスをテクスチャを化するからです。
対応するには、追加のプログラムが必要になります。

#謝辞

このアセットは、構造やプレビュー環境としては全般的にkeijiro氏の  
[unity-dither4444](https://github.com/keijiro/unity-dither4444)  
[unity-pvr-cleaner](https://github.com/keijiro/unity-pvr-cleaner)  
[unity-alphamask](https://github.com/keijiro/unity-alphamask)  
を参考にさせていただきました。  
またテスト画像としては[テラシュールウェア](http://terasur.blog.fc2.com)さんのご厚意によりunity-pvr-cleanerと同じものを使わせていただきました。  
ありがとうございました。  
なおこれらの著作は上記の方々に属しますので私的な使用以外は避けるようにしてください。  
