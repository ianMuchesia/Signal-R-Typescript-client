import { View } from 'react-native';
import React, { useEffect, useState, useCallback } from 'react';
import { GiftedChat } from 'react-native-gifted-chat';
import { useLocalSearchParams } from 'expo-router';
import AsyncStorage from '@react-native-async-storage/async-storage';
import * as SignalR from '@microsoft/signalr';
import axios from 'axios';
import { baseURL } from '@/baseURL';

const API_URL = baseURL + 'api/chat';

const Chat = () => {
  const { id: conversationId } = useLocalSearchParams<{ id: string }>();
  console.log(conversationId)
 
  const [messages, setMessages] = useState<any>([]);
  const [connection, setConnection] = useState<any>(null);
  const [currentUser, setCurrentUser] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    const setupConnection = async () => {
      try {
        const token = await AsyncStorage.getItem("userToken");
        const username = await AsyncStorage.getItem("username");
        if (!token || !username || !mounted) return;

        setCurrentUser(username);

        const newConnection = new SignalR.HubConnectionBuilder()
          .withUrl(`${baseURL}chathub`, {
            accessTokenFactory: () => token,
          })
          .withAutomaticReconnect([0, 2000, 5000, 10000])
          .configureLogging(SignalR.LogLevel.Information)
          .build();

          newConnection.onclose(() => console.log("SignalR connection closed."));
          newConnection.onreconnecting(() => console.log("Reconnecting to SignalR..."));
          newConnection.onreconnected(() => console.log("Reconnected to SignalR."));

        newConnection.on("ReceivePrivateMessage", (chatId, sender, message) => {
          console.log("SignalR Message Received -> Chat ID:", chatId, "Sender:", sender, "Message:", message);

          if (!mounted || chatId !== Number(conversationId))
           {
            console.log("Message received from different chat. Ignoring.");
            return;
           }
          console.log("Received message:", message);
          setMessages((prevMessages: any) =>
            GiftedChat.append(prevMessages, [
              {
                _id: Math.random().toString(),
                text: message,
                createdAt: new Date(),
                user: { _id: sender, name: sender },
              },
            ])
          );
        });

        await newConnection.start();
        console.log("SignalR connection started.");

        setConnection(newConnection);
      } catch (error) {
        console.error("Connection setup failed:", error);
      }
    };

    const fetchChatHistory = async () => {
      try {
        const token = await AsyncStorage.getItem("userToken");
        if (!token) return;

        const response = await axios.get(
          `${API_URL}/conversations/${conversationId}/messages`,
          {
            headers: { Authorization: `Bearer ${token}` },
          }
        );

        const formattedMessages = response.data.map((msg: any) => ({
          _id: msg.id.toString(),
          text: msg.content,
          createdAt: new Date(msg.timestamp),
          user: { _id: msg.senderUsername, name: msg.senderUsername },
        }));
        if (mounted) {
          setMessages(formattedMessages);
        }
      } catch (error) {
        console.error("Error fetching chat history:", error);
      }
    };

    setupConnection();
    fetchChatHistory();

    return () => {
      mounted = false;
      if (connection) {
        connection.stop();
      }
    };
  }, [conversationId]);

  const handleSend = useCallback(
    async (newMessages = []) => {
      console.log(newMessages)
      try {
        const token = await AsyncStorage.getItem("userToken");
        if (!token || !connection || !currentUser || !conversationId) return;

        const message = newMessages[0];

        console.log(message)

        await connection.invoke("SendPrivateMessage", parseInt(conversationId), (message as any).text);

        setMessages((prevMessages: any) => GiftedChat.append(prevMessages, newMessages));
      } catch (error) {
        console.error("Error sending message:", error);
      }
    },
    [connection, currentUser, conversationId]
  );

  return (
    <View style={{ flex: 1 }}>
      <GiftedChat
        messages={messages}
        onSend={(newMessages) => handleSend(newMessages as any)}
        user={{ _id: currentUser || '', name: currentUser || '' }}
      />
    </View>
  );
};

export default Chat;
