<template>
  <div class="submissions-page">
    <div class="eyebrow">Estudiante</div>
    <h1>Mis Envíos</h1>
    <p class="lead">Gestiona tus documentos de certificación de intercambio</p>

    <div class="submissions-container">
      <p>{{ greeting }}, {{ user.name }}!</p>
      <p style="margin-top: 20px">
        Envia tu documento
      </p>
    <div class = "submission-form">
      <form @submit.prevent = "submitForm">
        <input type ="file" accept = "application/pdf" @change="handleFileUpload"/>
        
        <button @click="submitForm" type="submit" class="btn-primary" :disabled="loading">Subir!</button>
        </form>
          <p v-if="uploadStatus">{{ uploadStatus }}</p>
    </div>
      <button @click="logout" class="btn-secondary">Cerrar Sesión</button>
    </div>
  </div>
</template>

<script>
import apiClient from "@/api/client";
import store from "@/store";

export default {
  name: "SubmissionsView",
  data() {
    return {
      user: store.auth.state.user || {},
      greeting: ["Bienvenido", "Hola"][Math.floor(Math.random() * 2)],
      pdfFile : null,
      uploadStatus: ''  

    };
  },
  methods: {
    logout() {
      store.auth.mutations.clearAuth();
      this.$router.push("/login");
    },
    handleFileUpload(event) {
      const file = event.target.files[0];
      if(file && file.type === 'application/pdf') {
        this.pdfFile = file;
        this.uploadStatus= '';
      }else{
        this.pdfFile = null;
        this.uploadStatus= 'El archivo debe ser en formato PDF';
      }
    },
    async submitForm(){
      
      console.log(this.pdfFile);
      if(!this.pdfFile) return;
      const formData = new FormData();
      formData.append('pdf', this.pdfFile);
      try{
        const response= await apiClient.sendDocument(formData);
        this.uploadStatus= response.data.message;
        console.log("fetch funca");
      }catch(err){
        this.uploadStatus= err.response.data.message
      }
    }
  },
};
</script>

<style scoped>
.submissions-page {
  max-width: 1120px;
  margin: 0 auto;
  padding: 40px 20px;
}

.eyebrow {
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--accent, #667eea);
  margin-bottom: 16px;
}

h1 {
  font-family: Georgia, serif;
  font-size: 40px;
  color: var(--text, #333);
  margin: 0 0 12px;
  font-weight: 500;
}

.lead {
  color: var(--text-secondary, #666);
  font-size: 18px;
  margin: 0 0 40px;
}

.submissions-container {
  background: rgba(102, 126, 234, 0.05);
  border-radius: 24px;
  padding: 32px;
  border: 1px solid var(--border, #e5e5e5);
}

.btn-secondary {
  padding: 12px 24px;
  background: var(--accent, #667eea);
  color: white;
  border: none;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  font-size: 14px;
}

.btn-secondary:hover {
  opacity: 0.9;
}

.btn-primary:hover {
  opacity: 0.9;
}
.btn-primary {
  padding: 12px 24px;
  margin: 20px;
  background: var(--accent, #667eea);
  color: white;
  border: none;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  font-size: 14px;
}
.submission-form {
  margin: 20px;
}
</style>
