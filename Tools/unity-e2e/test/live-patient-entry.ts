// Keep the existing CLI while selecting the scenario-specific investigation.
const graph = process.argv[3] ?? 'patient_a_critical';
if (graph === 'patient_a_critical') {
  await import('./live-patient-a-critical.ts');
} else if (graph === 'patient_b_c_ct') {
  await import('./live-patient-b-c-ct.ts');
} else {
  throw new Error(`Unsupported patient scenario: ${graph}`);
}
export {};
