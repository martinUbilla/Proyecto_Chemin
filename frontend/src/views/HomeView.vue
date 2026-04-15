<template>
  <section class="home-view">
    <div class="hero-card">
      <p class="eyebrow"></p>
      <h2>Backend and frontend foundation</h2>
      <p class="lead">
        Vue 2 is now wired to a centralized Axios client and the .NET 6 backend
        exposes a health endpoint. This is the base for authentication,
        submissions, and dashboard workflows.
      </p>

      <div class="actions">
        <button type="button" @click="checkBackend" :disabled="loading">
          {{ loading ? "Checking..." : "Check backend health" }}
        </button>
      </div>

      <div v-if="error" class="status status-error">
        {{ error }}
      </div>

      <div v-if="health" class="status status-success">
        <strong>{{ health.status }}</strong>
        <span>Service: {{ health.service }}</span>
        <span>Timestamp UTC: {{ health.timestampUtc }}</span>
      </div>
    </div>
  </section>
</template>

<script>
import { getBackendHealth } from "../api/client";

export default {
  name: "HomeView",
  data() {
    return {
      loading: false,
      health: null,
      error: "",
    };
  },
  methods: {
    async checkBackend() {
      this.loading = true;
      this.error = "";

      try {
        const response = await getBackendHealth();
        this.health = response.data;
      } catch (err) {
        this.health = null;
        this.error = "Unable to reach the backend health endpoint.";
        // eslint-disable-next-line no-console
        console.error(err);
      } finally {
        this.loading = false;
      }
    },
  },
  mounted() {
    this.checkBackend();
  },
};
</script>

<style scoped>
.home-view {
  display: grid;
  gap: 24px;
}

.hero-card {
  padding: 32px;
  border-radius: 28px;
  background: rgba(255, 250, 243, 0.9);
  border: 1px solid rgba(17, 75, 95, 0.12);
  box-shadow: 0 20px 60px rgba(31, 42, 55, 0.08);
}

.eyebrow {
  margin: 0 0 8px;
  text-transform: uppercase;
  letter-spacing: 0.16em;
  font-size: 0.72rem;
  color: #d17b0f;
}

h2 {
  margin: 0;
  font-size: 2.2rem;
  line-height: 1.05;
  letter-spacing: -0.04em;
}

.lead {
  max-width: 760px;
  margin: 16px 0 0;
  font-size: 1.02rem;
  line-height: 1.7;
  color: #4b5563;
}

.actions {
  margin-top: 24px;
}

button {
  appearance: none;
  border: 0;
  border-radius: 999px;
  padding: 12px 20px;
  font-weight: 700;
  color: #fff;
  background: linear-gradient(135deg, #114b5f, #0a6a7f);
  cursor: pointer;
}

button:disabled {
  opacity: 0.7;
  cursor: progress;
}

.status {
  display: grid;
  gap: 4px;
  margin-top: 24px;
  padding: 16px 18px;
  border-radius: 18px;
  border: 1px solid rgba(17, 75, 95, 0.12);
}

.status-success {
  background: rgba(17, 75, 95, 0.06);
}

.status-error {
  background: rgba(209, 123, 15, 0.08);
  color: #8f4b00;
}
</style>
