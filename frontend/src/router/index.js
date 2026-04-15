import Vue from "vue";
import Router from "vue-router";
import store from "@/store";
import HomeView from "../views/HomeView.vue";
import LoginView from "../views/LoginView.vue";

Vue.use(Router);

const router = new Router({
  mode: "history",
  base: process.env.BASE_URL,
  routes: [
    {
      path: "/login",
      name: "Login",
      component: LoginView,
      meta: { requiresAuth: false },
    },
    {
      path: "/",
      name: "home",
      component: HomeView,
      meta: { requiresAuth: false },
    },
    {
      path: "/submissions",
      name: "Submissions",
      component: () => import("../views/SubmissionsView.vue"),
      meta: { requiresAuth: true, roles: ["Student"] },
    },
    {
      path: "/dashboard",
      name: "Dashboard",
      component: () => import("../views/DashboardView.vue"),
      meta: { requiresAuth: true, roles: ["Admin"] },
    },
  ],
});

// Route guards: Check authentication and authorization
router.beforeEach((to, from, next) => {
  const isAuthenticated = store.auth.state.isAuthenticated;
  const userRole = store.auth.state.user?.role;

  // If route requires auth
  if (to.meta.requiresAuth) {
    if (!isAuthenticated) {
      // Not authenticated; redirect to login
      next("/login");
    } else if (to.meta.roles && !to.meta.roles.includes(userRole)) {
      // Wrong role; redirect home
      next("/");
    } else {
      // Authenticated and authorized
      next();
    }
  } else {
    // Route doesn't require auth
    if (isAuthenticated && to.path === "/login") {
      // Already logged in; redirect to appropriate page
      next(userRole === "Admin" ? "/dashboard" : "/submissions");
    } else {
      next();
    }
  }
});

export default router;
