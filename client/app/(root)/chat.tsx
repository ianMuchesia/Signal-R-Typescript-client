import React, { useState, useEffect, useCallback } from "react";
import { GiftedChat } from "react-native-gifted-chat";
import * as SignalR from "@microsoft/signalr";
import * as Notifications from "expo-notifications";
import AsyncStorage from "@react-native-async-storage/async-storage";
import axios from "axios";
import { baseURL } from "@/baseURL";

// Notification Handler
Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: true,
  }),
});

const ChatScreen = () => {
  const [messages, setMessages] = useState<any[]>([]);
  const [connection, setConnection] = useState<SignalR.HubConnection | null>(null);

  useEffect(() => {
    let mounted = true;

    const setupConnection = async () => {
      try {
        const token = await AsyncStorage.getItem("userToken");
        if (!token || !mounted) return;

        const newConnection = new SignalR.HubConnectionBuilder()
          .withUrl(`${baseURL}chatHub`, {
            accessTokenFactory: () => token,
          })
          .withAutomaticReconnect([0, 2000, 5000, 10000])
          .configureLogging(SignalR.LogLevel.Information)
          .build();

        newConnection.on("ReceiveMessage", async (user: string, msg: string) => {
          if (!mounted) return;

          // Push notification for new message
          await Notifications.scheduleNotificationAsync({
            content: {
              title: `New Message from ${user}`,
              body: msg,
              data: { user, msg },
            },
            trigger: null,
          });

          // Add message to chat
          const newMessage = {
            _id: Math.random().toString(),
            text: msg,
            createdAt: new Date(),
            user: { _id: user, name: user },
          };

          setMessages((prevMessages) => GiftedChat.append(prevMessages, [newMessage]));
        });

        await newConnection.start();
        console.log("Connected to SignalR");

        setConnection(newConnection);
      } catch (error) {
        console.error("Connection setup failed:", error);
      }
    };

    setupConnection();

    return () => {
      mounted = false;
      if (connection) {
        connection.stop();
      }
    };
  }, []);

  // Fetch chat history
  useEffect(() => {
    const fetchChatHistory = async () => {
      try {
       
        const response = await axios.get(`${baseURL}api/chat/history`);
        
        const formattedMessages = response.data.map((msg: any) => ({
          _id: msg.timestamp, // Assuming `id` is unique
          text: msg.content, // Adjust based on API response
          createdAt: new Date(msg.timestamp),
          user: { _id: 1, name: msg.username },
        }));

         setMessages(formattedMessages);
      } catch (error) {
        console.error("Error fetching chat history:", error);
      }
    };

    fetchChatHistory();
  }, []);

  // Send message function
  // const handleSend = useCallback(
  //   (newMessages: any = []) => {
  //     if (connection) {
  //       const message = newMessages[0];

  //       connection.invoke("SendMessage", message.user._id, message.text).catch((err) =>
  //         console.error("Message send error:", err)
  //       );

  //       setMessages((prevMessages) => GiftedChat.append(prevMessages, newMessages));
  //     }
  //   },
  //   [connection]
  // );

  const handleSend = useCallback(
    async (newMessages: any = []) => {
      if (!connection) {
        console.error("No connection available");
        return;
      }

      try {
        const message = newMessages[0];
        console.log("Sending message:", message); // Debug log

        // Format message for server
        const serverMessage = {
          username: message.user.name,
          content: message.text,
          timestamp: new Date().toISOString()
        };
        console.log(serverMessage)

        // Send to server
        await connection.invoke("SendMessage", serverMessage.content);
        console.log("Message sent successfully");

        // Update local state with formatted message
        const formattedMessage = {
          _id: Date.now().toString(),
          text: message.text,
          createdAt: new Date(),
          user: {
            _id: message.user._id,
            name: message.user.name
          }
        };

        setMessages(prevMessages => GiftedChat.append(prevMessages, [formattedMessage]));
      } catch (error) {
        console.error("Detailed send error:", error);
        console.log("Connection state:", connection.state);
      }
    },
    [connection]
  );

  return (
    <GiftedChat
      messages={messages}
      onSend={(newMessages) => handleSend(newMessages)}
      user={{ _id: 1, name:"ian" }} // Replace with actual user ID
    />
  );
};

export default ChatScreen;
