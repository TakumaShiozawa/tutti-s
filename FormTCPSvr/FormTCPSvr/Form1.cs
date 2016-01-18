/*
 * 参考：http://dobon.net/vb/dotnet/internet/tcpclientserver.html 
 * 
 * 2016年1月16日　作成
 * 2016年1月17日 08:00 追記　動作するまで
 * 2016年1月17日 10:17 追記　画面整理、複数行の入力実装、
 * 　　　　　　　　　　　　　チャットに時間と送った側の名前を追加
 * 2016年1月17日 10:35 追記　コメント追加
 * 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FormTCPSvr
{
    public partial class Form1 : Form
    {
        System.Net.Sockets.TcpClient client;
        System.Net.Sockets.TcpListener listener;
        System.Net.Sockets.NetworkStream ns;
        bool disconnected;
        bool socet;//通信の経路ができたら
        public Form1()
        {
            InitializeComponent();
            socet = false;
        }

        //通信開始ボタン(待機状態に)
        private void button1_Click(object sender, EventArgs e)
        {
            if (ns == null)
            {
                //ListenするIPアドレス
                //string ipString = "127.0.0.1";
                string ipString = textBox3.Text;
                System.Net.IPAddress ipAdd = System.Net.IPAddress.Parse(ipString);

                //ホスト名からIPアドレスを取得する時は、次のようにする
                //string host = "localhost";
                //System.Net.IPAddress ipAdd =
                //    System.Net.Dns.GetHostEntry(host).AddressList[0];
                //.NET Framework 1.1以前では、以下のようにする
                //System.Net.IPAddress ipAdd =
                //    System.Net.Dns.Resolve(host).AddressList[0];

                //Listenするポート番号
                int port = int.Parse(textBox4.Text);

                //TcpListenerオブジェクトを作成する
                listener = new System.Net.Sockets.TcpListener(ipAdd, port);

                //Listenを開始する
                listener.Start();
                label1.Text = "Listenを開始しました(" + ((System.Net.IPEndPoint)listener.LocalEndpoint).Address + ":" + ((System.Net.IPEndPoint)listener.LocalEndpoint).Port + ")。";

                //接続要求があったら受け入れる
                client = listener.AcceptTcpClient();
                label1.Text = "クライアント(" + ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address + ":" + ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Port + ")と接続しました。";

                //NetworkStreamを取得
                ns = client.GetStream();

                //読み取り、書き込みのタイムアウトを10秒にする
                //デフォルトはInfiniteで、タイムアウトしない
                //(.NET Framework 2.0以上が必要)
                ns.ReadTimeout = 10000;
                ns.WriteTimeout = 10000;
                socet = true;
            }

           
        }
        //切断ボタン
        private void button2_Click(object sender, EventArgs e)
        {
            if (ns != null)
            {
                //閉じる
                ns.Close();
                client.Close();
                Console.WriteLine("クライアントとの接続を閉じました。");

                //リスナを閉じる
                listener.Stop();
                Console.WriteLine("Listenerを閉じました。");

            }
            //Console.ReadLine();
        }

        //送信ボタン
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {

                if (!disconnected)
                {
                    //クライアントにデータを送信する
                    //クライアントに送信する文字列を作成
                    string sendMsg = "[Server]" + DateTime.Now.ToString() + "\r\n" + textBox2.Text + "\r\n\r\n";
                    //文字列をByte型配列に変換
                    System.Text.Encoding enc = System.Text.Encoding.UTF8;
                    byte[] sendBytes = enc.GetBytes(sendMsg + '\n');
                    //データを送信する
                    ns.Write(sendBytes, 0, sendBytes.Length);
                    textBox1.Text += sendMsg;
                    Console.WriteLine(sendMsg);

                    //カレット位置を末尾に移動
                    textBox1.SelectionStart = textBox1.Text.Length;
                    //テキストボックスにフォーカスを移動
                    textBox1.Focus();
                    //カレット位置までスクロール
                    textBox1.ScrollToCaret();
                }
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("接続されていません。", "エラー");
            }
        }

        //1秒ごとに受信
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (socet && client.Available > 0)
            {
                
                try
                {
                    //クライアントから送られたデータを受信する
                    
                    disconnected = false;
                    System.IO.MemoryStream ms = new System.IO.MemoryStream();
                    byte[] resBytes = new byte[256];
                    int resSize = 0;
                    do
                    {
                        //データの一部を受信する
                        resSize = ns.Read(resBytes, 0, resBytes.Length);
                        //Readが0を返した時はクライアントが切断したと判断
                        if (resSize == 0)
                        {
                            disconnected = true;
                            Console.WriteLine("クライアントが切断しました。");
                            break;
                        }
                        //受信したデータを蓄積する
                        ms.Write(resBytes, 0, resSize);
                        //まだ読み取れるデータがあるか、データの最後が\nでない時は、
                        // 受信を続ける
                    } while (ns.DataAvailable || resBytes[resSize - 1] != '\n');
                    //受信したデータを文字列に変換
                    System.Text.Encoding enc = System.Text.Encoding.UTF8;
                    string resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                    ms.Close();
                    //末尾の\nを削除
                    resMsg = resMsg.TrimEnd('\n');
                    //Console.WriteLine(resMsg);
                    textBox1.Text +=resMsg+"\r\n";
                    //カレット位置を末尾に移動
                    textBox1.SelectionStart = textBox1.Text.Length;
                    //テキストボックスにフォーカスを移動
                    textBox1.Focus();
                    //カレット位置までスクロール
                    textBox1.ScrollToCaret();
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

       
       
    }
}
