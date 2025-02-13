import React, { useState, useEffect } from "react";
import { View, FlatList, Text } from "react-native";
import axios from "axios";
import { baseURL } from "@/baseURL";

const API_URL = baseURL+"chat/history";

const ChatScreen = () => {
  const [messages, setMessages] = useState([]);

  useEffect(() => {
    const fetchChatHistory = async () => {
      try {
        const response = await axios.get(API_URL);
        setMessages(response.data);
      } catch (error) {
        console.error("Error fetching chat history:", error);
      }
    };

    fetchChatHistory();
  }, []);

  return (
    <View>
      <FlatList
        data={messages}
        keyExtractor={(item, index) => index.toString()}
        renderItem={({ item }) => (
          <Text>{`${(item as any).Username}: ${(item as any).Content}`}</Text>
        )}
      />
    </View>
  );
};

export default ChatScreen;
