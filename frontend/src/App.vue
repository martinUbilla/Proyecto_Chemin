<template>
  <div class="app-shell">
    <header class="app-header">
      <div>
        <p class="eyebrow">Proyecto Chemin</p>
      </div>
      <nav class="nav-links" aria-label="Primary">
        <router-link to="/login">Inicio</router-link>
      </nav>
    </header>

    <main class="app-main">
      <router-view />
    </main>
  </div>
</template>

<script>
export default {
  name: "App",
};
</script>

<style>
:root {
  color-scheme: light;
  --bg: #f5efe6;
  --panel: #fffaf3;
  --text: #1f2a37;
  --muted: #6b7280;
  --accent: #114b5f;
  --accent-2: #d17b0f;
  --border: rgba(17, 75, 95, 0.12);
}

* {
  box-sizing: border-box;
}

body {
  margin: 0;
  font-family: Georgia, "Times New Roman", serif;
  background: radial-gradient(
      circle at top left,
      rgba(17, 75, 95, 0.08),
      transparent 28%
    ),
    linear-gradient(180deg, #fbf7f0 0%, #f2eadf 100%);
  color: var(--text);
}

.app-shell {
  min-height: 100vh;
  padding: 24px;
}

.app-header,
.app-main {
  max-width: 1120px;
  margin: 0 auto;
}

.app-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 24px;
  padding: 20px 24px;
  border: 1px solid var(--border);
  border-radius: 24px;
  background: rgba(255, 250, 243, 0.85);
  backdrop-filter: blur(10px);
  box-shadow: 0 18px 50px rgba(31, 42, 55, 0.08);
}

.app-header h1 {
  margin: 4px 0 0;
  font-size: 2rem;
  letter-spacing: -0.03em;
}

.eyebrow {
  margin: 0;
  text-transform: uppercase;
  letter-spacing: 0.18em;
  font-size: 0.72rem;
  color: var(--accent-2);
}

.nav-links a {
  color: var(--accent);
  text-decoration: none;
  font-weight: 700;
}

.nav-links a.router-link-exact-active {
  text-decoration: underline;
  text-underline-offset: 4px;
}

.app-main {
  padding: 28px 0 8px;
}
</style>
