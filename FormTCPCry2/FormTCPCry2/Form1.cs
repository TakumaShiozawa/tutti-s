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

namespace FormTCPCry2
{
    public partial class Form1 : Form
    {
        System.Net.Sockets.TcpClient tcp;
        System.Net.Sockets.NetworkStream ns;
        bool socet;
        public Form1()
        {
            InitializeComponent();
            //垂直、水平両方のスクロールバーを表示"
            textBox1.ScrollBars = ScrollBars.Both;
            socet = false;//通信ができたら(つながったら)
        }

        //接続ボタン
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {

                //サーバーのIPアドレス（または、ホスト名）とポート番号
                //string ipOrHost = "127.0.0.1";
                //string ipOrHost = "127.0.0.1";
                string ipOrHost = textBox3.Text;
                //string ipOrHost = "localhost";
                //int port = 2001;
                int port = int.Parse(textBox4.Text);
                //TcpClientを作成し、サーバーと接続する
                tcp = new System.Net.Sockets.TcpClient(ipOrHost, port);
                /*
                Console.WriteLine("サーバー({0}:{1})と接続しました({2}:{3})。",
                    ((System.Net.IPEndPoint)tcp.Client.RemoteEndPoint).Address,
                    ((System.Net.IPEndPoint)tcp.Client.RemoteEndPoint).Port,
                    ((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Address,
                    ((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Port);
                */
                label1.Text = "サーバー(" + ((System.Net.IPEndPoint)tcp.Client.RemoteEndPoint).Address + ":" + ((System.Net.IPEndPoint)tcp.Client.RemoteEndPoint).Port + ")と接続しました("
                    + ((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Address + ":" + ((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Port + ")。";
                //NetworkStreamを取得する
                ns = tcp.GetStream();

                //読み取り、書き込みのタイムアウトを10秒にする
                //デフォルトはInfiniteで、タイムアウトしない
                //(.NET Framework 2.0以上が必要)
                ns.ReadTimeout = 10000;
                ns.WriteTimeout = 10000;
                socet = true;
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                MessageBox.Show(ex.ToString()+"\nサーバー側の準備ができていません", "エラー");
            }
        }

        //切断ボタン
        private void button2_Click(object sender, EventArgs e)
        {
            if(ns!=null){
                //閉じる
                ns.Close();
                tcp.Close();
                Console.WriteLine("切断しました。");

                //Console.ReadLine();
            }
        }

        //送信ボタン
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                //サーバーに送信するデータを入力してもらう
                //Console.WriteLine("文字列を入力し、Enterキーを押してください。");
                //string sendMsg = Console.ReadLine();

                //メッセージ取得
                string sendMsg = "[Client]" + DateTime.Now.ToString() + "\r\n" + textBox2.Text + "\r\n\r\n";

                //何も入力されなかった時は終了
                if (sendMsg == null || sendMsg.Length == 0)
                {
                    return;
                }

                //サーバーにデータを送信する
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
            catch (NullReferenceException)
            {
                MessageBox.Show("接続されていません。","エラー");
            }
            
        }

        //1秒ごとにデータを受信(データがあれば)
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (socet && tcp.Available > 0)//通信ができていて、Serverから何か来てれば
            {
                //サーバーから送られたデータを受信する
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                byte[] resBytes = new byte[256];
                int resSize = 0;
                do
                {
                    //データの一部を受信する
                    resSize = ns.Read(resBytes, 0, resBytes.Length);
                    //Readが0を返した時はサーバーが切断したと判断
                    if (resSize == 0)
                    {
                        Console.WriteLine("サーバーが切断しました。");
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
                Console.WriteLine(resMsg);
                textBox1.Text += resMsg+"\r\n";
                //カレット位置を末尾に移動
                textBox1.SelectionStart = textBox1.Text.Length;
                //テキストボックスにフォーカスを移動
                textBox1.Focus();
                //カレット位置までスクロール
                textBox1.ScrollToCaret();
            }
        }

       

    }
}
