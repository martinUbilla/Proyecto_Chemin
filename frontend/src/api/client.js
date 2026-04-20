import axios from "axios";
import store from "@/store";

const apiClient = axios.create({
  baseURL: process.env.VUE_APP_API_BASE_URL || "https://localhost:7238",
  timeout: 30000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Request interceptor: Add JWT token to every request
apiClient.interceptors.request.use(
  (config) => {
    const token = store.auth.state.token;
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// Response interceptor: Handle 401 Unauthorized
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token expired or invalid; clear auth and redirect to login
      store.auth.mutations.clearAuth();
      window.location.href = "/login";
    }
    return Promise.reject(error);
  },
);

export function getBackendHealth() {
  return apiClient.get("/api/health");
}

export function loginUser(email, password) {
  return apiClient.post("/api/auth/login", { email, password });
}

export function sendDocument(file) {
  return apiClient.post("/api/submissions", file, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
  });
}
export default apiClient;
