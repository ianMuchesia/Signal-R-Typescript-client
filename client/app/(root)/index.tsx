import React, { useState } from "react";
import { View, Text, TextInput, Button } from "react-native";
import { login, register } from "../../services/auth";
import { router } from "expo-router";
import AsyncStorage from '@react-native-async-storage/async-storage';


const home= () => {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  const handleLogin = async () => {
    try {
      const token = await login(username, password);
      console.log("JWT Token:", token);
      await AsyncStorage.setItem('userToken', token);
      await AsyncStorage.setItem('username', username);

      router.push("/users");
    } catch (err:any) {
      console.error(err)
      console.error(err.response.data.message);
    }
  };

  const handleRegister = async () => {
    try {
      await register(username, password);
      alert("User registered. Please login.");
    } catch (err:any) {
      console.log(err)
      console.error(err.response.data.message);
    }
  };

  return (
    <View>
      <Text>Username:</Text>
      <TextInput value={username} onChangeText={setUsername} />
      <Text>Password:</Text>
      <TextInput value={password} onChangeText={setPassword} secureTextEntry />
      <Button title="Login" onPress={handleLogin} />
      <Button title="Register" onPress={handleRegister} />
    </View>
  );
};

export default home;
