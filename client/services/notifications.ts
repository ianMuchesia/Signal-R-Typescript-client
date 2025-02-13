import * as Notifications from "expo-notifications";
import * as Permissions from "expo-permissions";

export const registerForPushNotifications = async () => {
  const { status } = await Permissions.askAsync(Permissions.NOTIFICATIONS);
  if (status !== "granted") return;

  const token = await Notifications.getExpoPushTokenAsync();
  console.log("Expo Push Token:", token.data);
  return token.data;
};
