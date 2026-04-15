<template>
  <div class="login-container">
    <div class="login-card">
      <div class="eyebrow">Proyecto Chemin</div>
      <h1>Inicia Sesión</h1>
      <p class="lead">Accede a tu cuenta para continuar</p>

      <form @submit.prevent="handleLogin" class="login-form">
        <div class="form-group">
          <label for="email">Correo Electrónico</label>
          <input
            v-model="form.email"
            type="email"
            id="email"
            placeholder="student@chemin.local o admin@chemin.local"
            required
          />
        </div>

        <div class="form-group">
          <label for="password">Contraseña</label>
          <input
            v-model="form.password"
            type="password"
            id="password"
            placeholder="Ingresa tu contraseña"
            required
          />
        </div>

        <button type="submit" class="btn-primary" :disabled="loading">
          {{ loading ? "Iniciando sesión..." : "Iniciar Sesión" }}
        </button>
      </form>

      <div v-if="error" class="error-message">
        {{ error }}
      </div>

      <div class="demo-users">
        <p class="text-muted">Usuarios de demostración:</p>
        <ul>
          <li>
            <strong>Estudiante:</strong> student@chemin.local (cualquier
            contraseña)
          </li>
          <li>
            <strong>Administrador:</strong> admin@chemin.local (cualquier
            contraseña)
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>

<script>
import { loginUser } from "@/api/client";
import store from "@/store";

export default {
  name: "LoginView",
  data() {
    return {
      form: {
        email: "",
        password: "",
      },
      loading: false,
      error: null,
    };
  },
  methods: {
    async handleLogin() {
      this.loading = true;
      this.error = null;

      try {
        const response = await loginUser(this.form.email, this.form.password);
        const { token, user } = response.data;

        // Store token and user in auth store
        store.auth.mutations.setToken(token);
        store.auth.mutations.setUser(user);
        store.auth.mutations.setIsAuthenticated(true);

        // Redirect based on role
        const redirectPath =
          user.role === "Admin" ? "/dashboard" : "/submissions";
        this.$router.push(redirectPath);
      } catch (err) {
        this.error =
          err.response?.data?.message ||
          "Error al iniciar sesión. Verifica tus credenciales.";
      } finally {
        this.loading = false;
      }
    },
  },
};
</script>

<style scoped>
.login-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 20px;
}

.login-card {
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(10px);
  border-radius: 24px;
  padding: 48px;
  max-width: 400px;
  width: 100%;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.8);
}

.eyebrow {
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: #667eea;
  margin-bottom: 16px;
}

h1 {
  font-family: Georgia, serif;
  font-size: 32px;
  color: #333;
  margin: 0 0 12px;
  font-weight: 500;
}

.lead {
  color: #666;
  font-size: 16px;
  margin: 0 0 32px;
}

.login-form {
  margin: 32px 0;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  font-size: 14px;
  font-weight: 600;
  color: #333;
  margin-bottom: 8px;
}

.form-group input {
  width: 100%;
  padding: 12px 16px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
  font-family: inherit;
  transition: border-color 0.2s, box-shadow 0.2s;
  box-sizing: border-box;
}

.form-group input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.btn-primary {
  width: 100%;
  padding: 12px 24px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  font-size: 16px;
  transition: transform 0.2s, box-shadow 0.2s;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 10px 25px rgba(102, 126, 234, 0.3);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error-message {
  background: #fee;
  border: 1px solid #fcc;
  color: #c33;
  padding: 12px 16px;
  border-radius: 8px;
  margin: 16px 0;
  font-size: 14px;
}

.demo-users {
  margin-top: 24px;
  padding: 16px;
  background: #f5f5f5;
  border-radius: 8px;
  font-size: 13px;
}

.demo-users p {
  margin: 0 0 8px;
  color: #666;
  font-weight: 600;
}

.demo-users ul {
  list-style: none;
  margin: 0;
  padding: 0;
  color: #666;
}

.demo-users li {
  margin: 4px 0;
}

.text-muted {
  color: #999;
}
</style>
