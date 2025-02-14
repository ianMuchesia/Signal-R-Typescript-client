import { baseURL } from "@/baseURL";
import axios from "axios";
import jwtDecode from "jwt-decode";

const API_URL = baseURL + "api/auth";

export const register = async (username:string, password:string) => {
  return axios.post(`${API_URL}/register`, { id:0,username, passwordHash:password });
};

export const login = async (username:string, password:string) => {
  const res = await axios.post(`${API_URL}/login`, {id:0, username, passwordHash:password });
  const token = res.data.token;
  return token;
};

export const getUsers = async (token:string) => {
  const res = await axios.get(`${API_URL}/users`, { headers: { Authorization: `Bearer ${token}` } });
  return res.data;
}

//export const decodeToken = (token:string) => jwtDecode(token);
