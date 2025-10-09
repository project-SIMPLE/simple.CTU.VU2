using System;
using UnityEngine;
using WebSocketSharp;


public abstract class WebSocketConnector : MonoBehaviour
{

     protected string host ;
     protected string port;

    protected bool UseMiddleware; 

    private WebSocket socket;


    protected bool UseHeartbeat = true; //only for middleware mode
    protected bool DesktopMode = false;
    protected bool fixedProperties = true;
    protected string DefaultIP = "192.168.0.50"; //"localhost";//"192.168.1.68"; 10.16.14.40 (test)"192.168.0.50"
    protected string DefaultPort = "8080";
    protected bool UseMiddlewareDM = true;

    protected int numErrorsBeforeDeconnection = 10;
    protected int numErrors = 0;

    void OnEnable() {
       
        port = PlayerPrefs.GetString("PORT"); 
        host = PlayerPrefs.GetString("IP");

        if (DesktopMode)
        {
            UseMiddleware = UseMiddlewareDM;
            host = "localhost";

            if (UseMiddleware)
            {
                port = "8080";
            }
            else 
            {
                port = "1000";
            }
            
        } else if (fixedProperties)
        {
            UseMiddleware = UseMiddlewareDM;
            host = DefaultIP;
            port = DefaultPort;
            
        }
        Debug.Log("WebSocketConnector host: " + host + " PORT: " + port + " MIDDLEWARE:" + UseMiddleware);

        /*
        Tạo một WebSocket tới ws://IP:Port/.

        Đăng ký các event handler trừu tượng — lớp con sẽ định nghĩa hành vi khi:

        Kết nối thành công (HandleConnectionOpen)

        Nhận được tin nhắn (HandleReceivedMessage)

        Kết nối bị đóng (HandleConnectionClosed)
        */
        socket = new WebSocket("ws://" + host + ":" + port + "/");
        socket.OnOpen += HandleConnectionOpen;
        socket.OnMessage += HandleReceivedMessage;
        socket.OnClose += HandleConnectionClosed;
    }

   void OnDestroy() {
       /*
        Khi GameObject bị hủy, đóng kết nối WebSocket để tránh rò rỉ tài nguyên.
        socket.Close() đóng kết nối WebSocket một cách an toàn.
    
       */ 
        socket.Close();
    }

    // ############################## HANDLERS ##############################
    /*Các hàm abstract (phải override ở lớp con)*/
    protected abstract void HandleConnectionOpen(object sender, System.EventArgs e);

    protected abstract void HandleReceivedMessage(object sender, MessageEventArgs e);

    protected abstract void HandleConnectionClosed(object sender, CloseEventArgs e);

    // #######################################################################
    /*Gửi tin nhắn tới server
        Gửi tin nhắn bất đồng bộ (async) tới server.
        successCallback nhận kết quả gửi thành công/thất bại.
        Có kiểm tra socket còn sống (IsAlive) và message hợp lệ trước khi gửi.
    */
    protected void SendMessageToServer(string message, Action<bool> successCallback)
    {

        if (!socket.IsAlive)
        {
            // Debug.LogError("WebSocket is not connected. Cannot send message: " + message);
            // successCallback(false);
            return;
        }
        if (message == null || message.Length == 0)
        {
            // Debug.LogError("Message is null or empty. Cannot send message.");
            // successCallback(false);
            return;
        }
        socket.SendAsync(message, successCallback);
       /*
        Hàm này gửi dữ liệu từ Unity lên server qua WebSocket.
        message là chuỗi bạn muốn gửi (thường là JSON hoặc lệnh).
        socket.SendAsync(...) là lệnh thực tế gửi đi qua mạng. 
        successCallback nhận biết việc gửi có thành công hay không.
        Nếu socket không kết nối (IsAlive == false) hoặc message rỗng, hàm sẽ không gửi gì cả.

        */
    }

    protected WebSocket GetSocket() {
        return socket;
    }

    /*Hàm tiện ích kiểm tra IP*/
    private bool ValidIp(string ip)
    {
        if (ip == null || ip.Length == 0) return false;
        string[] ipb = ip.Split(".");
        return (ipb.Length != 4);
    }
}
