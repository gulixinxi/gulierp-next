import { pinyin } from 'pinyin-pro';

const MAX_MNEMONIC_LENGTH = 40;

export function generateMnemonicCode(name: string | null | undefined): string {
  const source = name?.trim();
  if (!source) return '';

  const firstLetters = pinyin(source, {
    pattern: 'first',
    toneType: 'none',
    type: 'array',
    nonZh: 'consecutive',
  });

  return firstLetters
    .join('')
    .toUpperCase()
    .replace(/[^A-Z0-9]/g, '')
    .slice(0, MAX_MNEMONIC_LENGTH);
}

export function shouldRefreshMnemonic(
  currentMnemonic: string | null | undefined,
  previousSuggestion: string,
  userEdited: boolean,
): boolean {
  if (userEdited) return false;
  const current = currentMnemonic?.trim() ?? '';
  return current === '' || current === previousSuggestion;
}
