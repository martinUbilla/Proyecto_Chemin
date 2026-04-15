// Simple Pinia-like auth store pattern for Vue 2
// Manages auth token and user state

const state = {
  token: localStorage.getItem("authToken") || null,
  user: localStorage.getItem("authUser")
    ? JSON.parse(localStorage.getItem("authUser"))
    : null,
  isAuthenticated: !!localStorage.getItem("authToken"),
  loading: false,
  error: null,
};

const mutations = {
  setToken(token) {
    state.token = token;
    if (token) {
      localStorage.setItem("authToken", token);
    } else {
      localStorage.removeItem("authToken");
    }
  },

  setUser(user) {
    state.user = user;
    if (user) {
      localStorage.setItem("authUser", JSON.stringify(user));
    } else {
      localStorage.removeItem("authUser");
    }
  },

  setIsAuthenticated(isAuth) {
    state.isAuthenticated = isAuth;
  },

  setLoading(loading) {
    state.loading = loading;
  },

  setError(error) {
    state.error = error;
  },

  clearAuth() {
    state.token = null;
    state.user = null;
    state.isAuthenticated = false;
    state.error = null;
    localStorage.removeItem("authToken");
    localStorage.removeItem("authUser");
  },
};

const actions = {
  login({ commit }, { token, user }) {
    commit("setToken", token);
    commit("setUser", user);
    commit("setIsAuthenticated", true);
    commit("setError", null);
  },

  logout({ commit }) {
    commit("clearAuth");
  },

  setError({ commit }, error) {
    commit("setError", error);
  },
};

const getters = {
  isAdmin: () => state.user?.role === "Admin",
  isStudent: () => state.user?.role === "Student",
  authHeader: () => ({
    Authorization: `Bearer ${state.token}`,
  }),
};

export default {
  state,
  mutations,
  actions,
  getters,
};
