//
// Word-level diff of two short texts (a few paragraphs), for showing suggested edits.
//

export type DiffPart = { type: 'same' | 'del' | 'ins', text: string };

// words, runs of whitespace, and all other characters by one
const tokenize = (text: string) => text.match(/[\p{L}\p{N}]+|\s+|[^\p{L}\p{N}\s]/gu) ?? [];

export function diffWords(oldText: string, newText: string): DiffPart[] {
    const a = tokenize(oldText), b = tokenize(newText);

    // common prefix and suffix do not need to go into the table
    let start = 0;
    while (start < a.length && start < b.length && a[start] === b[start]) start++;
    let endA = a.length, endB = b.length;
    while (endA > start && endB > start && a[endA - 1] === b[endB - 1]) { endA--; endB--; }

    // longest common subsequence of the rest
    const n = endA - start, m = endB - start;
    const lcs: Uint32Array[] = [];
    for (let i = 0; i <= n; i++) lcs.push(new Uint32Array(m + 1));
    for (let i = n - 1; i >= 0; i--) {
        for (let j = m - 1; j >= 0; j--) {
            lcs[i]![j] = a[start + i] === b[start + j]
                ? lcs[i + 1]![j + 1]! + 1
                : Math.max(lcs[i + 1]![j]!, lcs[i]![j + 1]!);
        }
    }

    const parts: DiffPart[] = [];
    const push = (type: DiffPart['type'], text: string) => {
        if (!text) return;
        const last = parts[parts.length - 1];
        if (last && last.type === type) last.text += text;
        else parts.push({type, text});
    };

    push('same', a.slice(0, start).join(''));
    let i = 0, j = 0;
    while (i < n || j < m) {
        if (i < n && j < m && a[start + i] === b[start + j]) {
            push('same', a[start + i]!);
            i++; j++;
        } else {
            // deletions before insertions within one changed stretch, reads better
            let deleted = '', inserted = '';
            while ((i < n || j < m) && !(i < n && j < m && a[start + i] === b[start + j])) {
                if (j >= m || (i < n && lcs[i + 1]![j]! >= lcs[i]![j + 1]!)) deleted += a[start + i++]!;
                else inserted += b[start + j++]!;
            }
            push('del', deleted);
            push('ins', inserted);
        }
    }
    push('same', a.slice(endA).join(''));
    return parts;
}
