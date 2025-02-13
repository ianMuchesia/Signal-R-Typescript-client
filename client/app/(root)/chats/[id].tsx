import { View, Text } from 'react-native'
import React from 'react'
import { useLocalSearchParams } from 'expo-router'

const Chat = () => {
    const { id} = useLocalSearchParams<{id:string}>();
  return (
    <View>
      <Text>Chat</Text>
    </View>
  )
}

export default Chat