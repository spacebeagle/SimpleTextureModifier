[Unity]テクスチャサイズ削減のためのシンプルなアセットポストプロセッサ
===================================================================

Unity上で主にテクスチャサイズを削減するためのシンプルなアセットポストプロセッサを作りました。
テクスチャに適切なラベルを設定することにより、テクスチャモデファイアがそれを認識してテクスチャフォーマットの変換を行います。
ラベルはProjectのテクスチャファイル上でマウスの右ボタンを押して、Texture Utilから選択して設定することができます。
ラベルは３つのカテゴリにわかれています。
"Texture Effecter"、"Texture Modifier"、"Texture Output"です。
それぞれ１種類の機能を選択できます。
プルダウンで選択可能な機能は以下のようになっています。

Clear Texture Effecter Label
Set Label PremultipliedAlpha
Set Label AlphaBleed

Clear Texture Modifier Label
Set Label FloydSteinberg
Set Label Reduced16bits

Clear Texture Output Label
Set Label Convert 16bits
Set Label Convert Compressed
Set Label Convert Compressed no alpha
Set Label Convert Compressed with alpha
Set Label Texture PNG
Set Label Texture JPG

テクスチャ圧縮形式PVRTCは、アルファあり無しで同一のbits/pixelを実現しています。
これは圧縮ブロック(4x4)ごとに、フォーマットをRGBにするかRGBAにするか、"色が黒でアルファ値ゼロ"の特殊なピクセル(DirectXのDXT1のようなパンチスルー用)を指定するビット並びを使えるか、をビットフラグによって指定することで可能になっています。
ただし、これらはDXT5のようなアルファ値のためピクセルごとに色から独立した4bitデータを持たせる方法に比べると劣化か大きいのも事実です。

Imagination Technologies社のテクスチャツールPVRTexToolはこうした劣化を軽減するために二つの方法を用意しています、一つはテクスチャのalpha bleeding、もう一つはPremultiplied Alpha(事前乗算済みアルファ)という機能です。
これらは、それなりに効果が得られそうではありますが、Unity標準ではうまく動いていません。そこで実現するアセットを用意しました。

なお、今回のプロジェクトが正しく動作するためにはビルドセッティングのプラットフォームがiOSになっている必要があります。各自切り替えてから試すようにしてください。

様々な変換の例
--------------
このプロジェクトは2枚のイメージに対してそれぞれ5種類の異なった加工を行い表示するものです。  
名前の末尾が  
1.Bleedのものは、GUIモードのテクスチャをalpha bleedingしてアルファ付き4bit PVRTC化したもの、  
2.DefaultはGUIモードのテクスチャをそのまま、アルファ付き4bit PVRTC化したもの、  
3.PMAはGUIモードのテクスチャをPremultiplied Alpha化し、アルファ付き4bit PVRTC化したものをPremultiplied Alpha用のシェーダーを使って表示したもの、  
4.PVRToolは前述のPVRTexToolによってalpha bleeding処理しておいたGUIモードのテクスチャをそのまま、アルファ付き4bit PVRTC化したもの、  
5.TruecolorはGUIモードのテクスチャをそのまま、フルカラーテクスチャ化したもの、  

となっています。  
これらはAssetPostprocessorを使用して処理されていて、テクスチャのインポートモードがGUIでテクスチャファイル名の末尾が"Bleed.png"の場合alpha bleedingが、"PMA.png"の場合Premultiplied Alphaが処理されます。  
結果をPVRTCにしたいときは出力の形式を自分でPVRTCに切り替えておかなければなりません。  


PVRTC、画質悪化の原因
------------
特に劣化か激しく見えるDefaultのテクスチャですが、これには理由があります。  
GUIモードのテクスチャは裏側でAlpha Is Transparencyをセットしています。  
これはアルファで抜きが入ったテクスチャに対して高速なalpha bleeding処理をおこなうというフラグなのですが、このbleeding処理とPVRTCの相性がとても悪く透明部分にゴミが出てしまうようなのです。  
alpha bleeding処理とは[UnityのFAQにある回答](http://docs.unity3d.com/Documentation/Manual/HOWTO-alphamaps.html)の処理をUnity内でおこなえるようにしたもので、テクスチャの不透明部分の色を透明部分にはみ出させるように処理することによって、不透明部分の色が透明部分の影響を受けないようにするものです。  
Unityの処理はこれを比較的少ない不透明ピクセルのサンプリングよっておこなっているため、bleedingが高い周波数となってゴミがでてしまうのではないでしょうか。テクスチャ圧縮は狭い領域に広い帯域の色値が入るような画像に対しては効率が良い圧縮はできないからです。  
とりあえずUnityのalpha bleeding処理を切るために、GUIモードからAdvancedモードに切り替えて、Alpha Is Transparencyをオフにしてやればこうしたゴミが大幅に軽減するのが確認できるでしょう。  
しかしこの場合、今度は抜きの周辺のドットに黒いカゲのようなものが発生してしまいます。これはアルファ値が0の完全に透明な領域の色値が不透明な領域に影響を与えてしまうためです。  

2つの方法による改善
----------------------------
alpha bleeding処理自体は、エッジのクオリティを改善するために有効な手段なのは確かでしょう。  
そこで不透明ピクセルのサンプリング数を増やし、それらを平均化することによりスムーズな画像になるようにしました。加えてalpha bleedingの終了部、bleedingが終わって完全な透明部へ移行する部分にゴミがでがちな気がしたので、ここを背景色へのグラデーション化して軽減できるようにしてみました。  
これが末尾がBleedのサンプルです。  

また、まったく異なる発想で、Premultiplied Alphaを使用した方法もテストしてみました。  
末尾がPMAのものがそれです。  
事前処理としては軽いのですが、実行時には専用のシェーダーが必要になります。
それはプロジェクト内に用意しました。  
Premultiplied Alphaに関してはxnaのオフィシャルブログとの[乗算済みアルファとは？ その１:補間アルファの問題点](http://blogs.msdn.com/b/ito/archive/2010/07/10/what-is-the-premultilied-alpha-part-1.aspx)、[乗算済みアルファとは？ その2: コンポジション](http://blogs.msdn.com/b/ito/archive/2011/09/01/compositoin-with-the-premultiplied-alpha.aspx)あたりに色々書いてあります。  
これがテクスチャ圧縮に有効な理由としては、アルファチャンネルが0に近づくにつれ、ピクセルの色値も除算されていって情報量が減っていく、、前述のようにPVRTCにもDXT1の"色が黒でアルファ値ゼロ"の抜き専用のピクセルがあり。アルファ値がゼロの場合は事前に色値にもゼロが乗算され、色値も黒になって、この専用のピクセルが使われるのが期待できる、といったあたりにあるのではないでしょうか。  

なおalpha bleedingとPremultiplied Alphaは仕組み上重複してかけることはできません。
どちらかを選択することになります。

今後の課題
----------
UnityでBleedをおこなう処理は、計算量が多く重めのものとなっています。  
改善としては末尾、PVRToolのテクスチャが参考になるかもしれません。  
これは、PVRTexToolによるalpha bleeding処理を評価するはずのものなのですが、このままでは正常に動作しません。正しくは、テクスチャのAdvancedモードに切り替えて、Alpha Is Transparencyをオフにしてやる必要があります。  
GUIモードのままだとPVRTexToolの効果はUnityのalpha bleedingに上書きされて無くなってしまうはずなのですが、実際にはUnityのalpha bleedingのみの処理であるDefaultと比較するとかなりゴミの軽減が見られます。おそらくアルファ値が0より大きく1.0未満な、半透明な領域に対しての処理がUnityのalpha bleedingは消極的、PVRTexTool積極的になっているせいで、エッジの半透明な部分にPVRTexToolによる処理が残ってしまっているのではないかと考えられます。  
このことから類推するにゴミに大きく影響するのはエッジのわずかなピクセルのみで、それ以外は高速で簡易な処理でいいということにもなります。  

著作権フリー画像ではないので結果をここでは公開できませんが、多くの画像において、Premultiplied Alphaは安定した品質が保てる感触がありました。  
xna 4.0ではPremultiplied Alphaが標準ということも考えると、圧縮、非圧縮にかかわらずこれを生かす手を考えてみるのもいいかもしれません。  

謝辞
--------------------------
このアセットは、構造やプレビュー環境としては全般的にkeijiro氏の[unity-pvr-cleaner](https://github.com/keijiro/unity-pvr-cleaner)を参考にさせていただきました。  
またテスト画像としては[テラシュールウェア](http://terasur.blog.fc2.com)さんのご厚意によりunity-pvr-cleanerと同じものを使わせていただきました。  
ありがとうございました。  
なおこれらの著作は上記の方々に属しますので私的な使用以外は避けるようにしてください。  
