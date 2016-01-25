using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DigitalSignage_Server
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Net.Sockets.TcpClient client;
        System.Net.Sockets.TcpListener listener;
        System.Net.Sockets.NetworkStream ns;
        bool socet;
        bool disconnected;

        public MainWindow()
        {
            InitializeComponent();
            socet = false;
        }
        //通信開始ボタン(待機状態に)
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (socet == false)//つながってないなら
            {
                try
                {
                    //ListenするIPアドレス
                    //string ipString = "127.0.0.1";
                    string ipString = textBox4.Text;
                    System.Net.IPAddress ipAdd = System.Net.IPAddress.Parse(ipString);

                    //ホスト名からIPアドレスを取得する時は、次のようにする
                    //string host = "localhost";
                    //System.Net.IPAddress ipAdd =
                    //    System.Net.Dns.GetHostEntry(host).AddressList[0];
                    //.NET Framework 1.1以前では、以下のようにする
                    //System.Net.IPAddress ipAdd =
                    //    System.Net.Dns.Resolve(host).AddressList[0];

                    //Listenするポート番号
                    int port = int.Parse(textBox5.Text);

                    //TcpListenerオブジェクトを作成する
                    listener = new System.Net.Sockets.TcpListener(ipAdd, port);

                    //Listenを開始する
                    listener.Start();
                    State.Content = "Listenを開始しました(" + ((System.Net.IPEndPoint)listener.LocalEndpoint).Address + ":" + ((System.Net.IPEndPoint)listener.LocalEndpoint).Port + ")。";

                    //接続要求があったら受け入れる
                    client = listener.AcceptTcpClient();
                    State.Content = "クライアント(" + ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address + ":" + ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Port + ")と接続しました。";

                    //NetworkStreamを取得
                    ns = client.GetStream();

                    //読み取り、書き込みのタイムアウトを10秒にする
                    //デフォルトはInfiniteで、タイムアウトしない
                    //(.NET Framework 2.0以上が必要)
                    ns.ReadTimeout = 10000;
                    ns.WriteTimeout = 10000;
                    socet = true;
                }
                catch (FormatException)
                {
                    MessageBox.Show("IPアドレス、ポート番号を正しく入力してください(半角)", "エラー");
                }
                catch (System.Net.Sockets.SocketException)
                {
                    MessageBox.Show("IPアドレスが間違ってます。\ncmd.exeで調べてください\n(ipconfig)", "エラー");
                }
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (!disconnected)
                {
                    //クライアントにデータを送信する
                    //クライアントに送信する文字列を作成
                    string sendMsg =  textBox1.Text +","+ textBox2.Text +","+ textBox3.Text+ "\r\n";
                    //文字列をByte型配列に変換
                    System.Text.Encoding enc = System.Text.Encoding.UTF8;
                    byte[] sendBytes = enc.GetBytes(sendMsg + '\n');
                    //データを送信する
                    ns.Write(sendBytes, 0, sendBytes.Length);
                    //textBox1.Text += sendMsg;
                    //Console.WriteLine(sendMsg);

                }
            }
            catch (Exception)
            {
                MessageBox.Show("接続されていません。", "エラー");
                State.Content += "―メッセージを送れませんでした―\r\n";
                
            }

            if (socet == true)
            {
                //閉じる
                ns.Close();
                client.Close();
                Console.WriteLine("クライアントとの接続を閉じました。");

                //リスナを閉じる
                listener.Stop();
                Console.WriteLine("Listenerを閉じました。");

                State.Content = "クライアントとの接続を閉じました。";
                socet = false;

            }
        }

       
       
    }
}
