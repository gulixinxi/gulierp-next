<template>
  <main class="shell">
    <section class="status-panel">
      <div>
        <p class="eyebrow">GuliERP Next</p>
        <h1>Greenfield Bootstrap</h1>
      </div>
      <el-tag type="success" effect="dark">GULIERP_GREENFIELD_BOOTSTRAPPED</el-tag>
      <el-divider />
      <el-descriptions :column="1" border>
        <el-descriptions-item label="Backend">.NET 10 + ASP.NET Core + EF Core + PostgreSQL</el-descriptions-item>
        <el-descriptions-item label="Frontend">Vue 3 + TypeScript + Vite + Element Plus + Pinia + Vue Router</el-descriptions-item>
        <el-descriptions-item label="Business Gate">Spec -> UX Prototype -> User Approval -> API Contract -> Implementation</el-descriptions-item>
        <el-descriptions-item label="Scope">Foundation boundary only. No formal ERP document implementation in G0.</el-descriptions-item>
      </el-descriptions>
    </section>
  </main>
</template>

