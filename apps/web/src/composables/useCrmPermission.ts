import { useAuthStore } from '../stores/auth';

// The server supplies active grants for the selected tenant/company. This is
// only a presentation hint; every API mutation still checks its own policy.
export function useCrmPermission() {
  const auth = useAuthStore();
  return (code: string): boolean => auth.user?.permissionCodes?.includes(code) === true;
}
