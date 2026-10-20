<template>
  <main class="password-page">
    <el-card class="password-card">
      <h1>首次登录需要修改密码</h1>
      <p>请设置您自己的密码。完成后重新登录，才能使用 ERP 和 CRM。</p>
      <el-form label-position="top" @submit.prevent="submit">
        <el-form-item label="当前密码"><el-input v-model="current" type="password" autocomplete="current-password" show-password /></el-form-item>
        <el-form-item label="新密码"><el-input v-model="next" type="password" autocomplete="new-password" show-password /></el-form-item>
        <el-form-item label="确认新密码"><el-input v-model="confirm" type="password" autocomplete="new-password" show-password /></el-form-item>
        <p>使用 15–128 个字符的密码短语。</p>
        <el-button type="primary" :loading="busy" @click="submit">保存并重新登录</el-button>
        <el-button @click="signOut">退出登录</el-button>
      </el-form>
    </el-card>
  </main>
</template>

<script setup lang="ts">
import { onBeforeUnmount, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { changePassword } from '../../api/auth';
import { useAuthStore } from '../../stores/auth';
const auth = useAuthStore();
const current = ref('');
const next = ref('');
const confirm = ref('');
const busy = ref(false);
async function submit() {
  if (busy.value) return;
  if (!current.value || next.value.length < 15 || next.value.length > 128 || next.value !== confirm.value) {
    ElMessage.warning('请填写当前密码，并确认 15–128 个字符的新密码'); return;
  }
  busy.value = true;
  try {
    await changePassword(current.value, next.value);
    current.value = next.value = confirm.value = '';
    auth.finishPasswordChange();
    ElMessage.success('密码已修改，请重新登录');
  } catch (error) {
    ElMessage.error((error as { code?: string }).code === 'current_password_invalid'
      ? '当前密码不正确' : '修改失败，请检查新密码');
  } finally { busy.value = false; }
}
async function signOut() { await auth.signOut(); }
onBeforeUnmount(() => { current.value = next.value = confirm.value = ''; });
</script>

<style scoped>
.password-page { min-height: 100vh; display: grid; place-items: center; background: #f8fafc; }
.password-card { width: min(440px, calc(100vw - 32px)); }
</style>
