import { describe, it, expect } from 'vitest';
import { reorder, shuffle, applyOrder } from '../utils';

describe('reorder', () => {
  it('moves the first element to the last position', () => {
    const arr = ['a', 'b', 'c'];
    reorder(arr, 0, 2);
    expect(arr).toEqual(['b', 'c', 'a']);
  });

  it('moves the last element to the first position', () => {
    const arr = ['a', 'b', 'c'];
    reorder(arr, 2, 0);
    expect(arr).toEqual(['c', 'a', 'b']);
  });

  it('moves a middle element to the front', () => {
    const arr = ['a', 'b', 'c', 'd'];
    reorder(arr, 2, 0);
    expect(arr).toEqual(['c', 'a', 'b', 'd']);
  });

  it('does nothing when fromIndex equals toIndex', () => {
    const arr = ['a', 'b', 'c'];
    reorder(arr, 1, 1);
    expect(arr).toEqual(['a', 'b', 'c']);
  });

  it('does nothing for a single-element array', () => {
    const arr = ['a'];
    reorder(arr, 0, 0);
    expect(arr).toEqual(['a']);
  });

  it('does nothing when fromIndex is out of bounds', () => {
    const arr = ['a', 'b', 'c'];
    reorder(arr, 5, 0);
    expect(arr).toEqual(['a', 'b', 'c']);
  });

  it('does nothing when toIndex is out of bounds', () => {
    const arr = ['a', 'b', 'c'];
    reorder(arr, 0, 5);
    expect(arr).toEqual(['a', 'b', 'c']);
  });

  it('does nothing when fromIndex is negative', () => {
    const arr = ['a', 'b', 'c'];
    reorder(arr, -1, 0);
    expect(arr).toEqual(['a', 'b', 'c']);
  });

  it('works with numeric arrays', () => {
    const arr = [1, 2, 3, 4];
    reorder(arr, 3, 1);
    expect(arr).toEqual([1, 4, 2, 3]);
  });
});

describe('shuffle', () => {
  it('returns an empty array for an empty input', () => {
    expect(shuffle([])).toEqual([]);
  });

  it('returns a new array containing the single element for a length-1 input', () => {
    const result = shuffle(['a']);
    expect(result).toEqual(['a']);
  });

  it('returns a new array containing both original elements for a length-2 input', () => {
    const result = shuffle(['a', 'b']);
    expect(result).toHaveLength(2);
    expect(result).toContain('a');
    expect(result).toContain('b');
  });

  it('returns all original elements with no duplicates and no missing items', () => {
    const input = ['a', 'b', 'c', 'd', 'e'];
    const result = shuffle(input);
    expect(result).toHaveLength(input.length);
    expect([...result].sort()).toEqual([...input].sort());
  });

  it('does not mutate the original array', () => {
    const input = ['a', 'b', 'c'];
    const inputCopy = [...input];
    shuffle(input);
    expect(input).toEqual(inputCopy);
  });

  it('returns a new array instance', () => {
    const input = ['a', 'b', 'c'];
    const result = shuffle(input);
    expect(result).not.toBe(input);
  });

  it('preserves generic type (numbers)', () => {
    const nums = [1, 2, 3, 4];
    const result = shuffle(nums);
    expect(result).toHaveLength(4);
    result.forEach(n => expect(typeof n).toBe('number'));
  });
});

describe('applyOrder', () => {
  const exercises = [
    { exerciseId: 'a', name: 'A' },
    { exerciseId: 'b', name: 'B' },
    { exerciseId: 'c', name: 'C' },
  ];

  it('returns exercises in the specified order', () => {
    const result = applyOrder(exercises, ['c', 'a', 'b']);
    expect(result.map(e => e.exerciseId)).toEqual(['c', 'a', 'b']);
  });

  it('ignores IDs not present in the exercises array', () => {
    const result = applyOrder(exercises, ['c', 'unknown-id', 'a', 'b']);
    expect(result.map(e => e.exerciseId)).toEqual(['c', 'a', 'b']);
  });

  it('appends exercises not present in the order list to the end', () => {
    const result = applyOrder(exercises, ['b']);
    expect(result.map(e => e.exerciseId)).toEqual(['b', 'a', 'c']);
  });

  it('returns a copy of the exercises array when order is empty', () => {
    const result = applyOrder(exercises, []);
    expect(result.map(e => e.exerciseId)).toEqual(['a', 'b', 'c']);
  });

  it('returns a copy of the exercises array when no order IDs are valid', () => {
    const result = applyOrder(exercises, ['x', 'y', 'z']);
    expect(result.map(e => e.exerciseId)).toEqual(['a', 'b', 'c']);
  });

  it('does not mutate the original exercises array', () => {
    const input = [{ exerciseId: 'a', name: 'A' }, { exerciseId: 'b', name: 'B' }];
    applyOrder(input, ['b', 'a']);
    expect(input[0].exerciseId).toBe('a');
    expect(input[1].exerciseId).toBe('b');
  });

  it('preserves the full exercise object (not just the ID)', () => {
    const result = applyOrder(exercises, ['b', 'a', 'c']);
    expect(result[0]).toEqual({ exerciseId: 'b', name: 'B' });
  });
});
