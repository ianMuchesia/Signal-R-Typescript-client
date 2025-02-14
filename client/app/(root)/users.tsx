import { View, Text, TouchableOpacity, StyleSheet, Alert } from "react-native";
import React, { useEffect } from "react";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { getUsers } from "@/services/auth";
import { router } from "expo-router";
import { baseURL } from "@/baseURL";

const Users = () => {
  const [users, setUsers] = React.useState<any>([]);

  //run use effect with isMounted to prevent memory leaks
  //Async storage to get the token please
  useEffect(() => {
    let isMounted = true;
    const fetchUsers = async () => {
      try {
        console.log("fetching users");
        const token = await AsyncStorage.getItem("userToken");
        if (!token) return;
        const users = await getUsers(token);
        console.log(users);
        if (isMounted) setUsers(users);
      } catch (error) {
        console.log(error);
      }
    };
    fetchUsers();
    return () => {
      isMounted = false;
    };
  }, []);

  const startChat = async (otherUsername: string) => {
    const token = await AsyncStorage.getItem("userToken");

    try {
      const response = await fetch(baseURL + "api/chat/conversations", {
        method: "POST",
        body: JSON.stringify({ otherUsername }),
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
      });

      const conversation = await response.json();
      console.log("conversationId:", conversation.id);
      router.push(`/chats/${conversation.id}`);
    } catch (error) {
      console.log(error);
      Alert.alert("Error", "Couldn't start chat");
    }
  };

  return (
    <View style={styles.container}>
      {users.map((user: any) => (
        <TouchableOpacity
          key={user.username}
          style={styles.button}
          onPress={() => startChat(user.username)}
        >
          <Text style={styles.buttonText}>{user.username}</Text>
        </TouchableOpacity>
      ))}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
  },
  text: {
    fontSize: 20,
    color: "black",
  },
  button: {
    padding: 10,
    margin: 10,
    backgroundColor: "blue",
  },
  buttonText: {
    color: "white",
  },
});

export default Users;
