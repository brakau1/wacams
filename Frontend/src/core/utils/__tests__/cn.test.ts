import { describe, it, expect } from 'vitest'
import { cn } from '../cn'

describe('cn', () => {
  it('should return merged classes', () => {
    const result = cn('bg-red-500', 'text-white')
    expect(result).toContain('bg-red-500')
    expect(result).toContain('text-white')
  })

  it('should handle empty inputs', () => {
    expect(cn()).toBe('')
    expect(cn(undefined)).toBe('')
    expect(cn(null)).toBe('')
  })

  it('should merge conflicting classes (last wins)', () => {
    const result = cn('bg-red-500', 'bg-blue-500')
    expect(result).not.toContain('bg-red-500')
    expect(result).toContain('bg-blue-500')
  })

  it('should handle falsy values in array', () => {
    const result = cn('text-lg', false && 'text-xl')
    expect(result).toContain('text-lg')
    expect(result).not.toContain('text-xl')
  })

  it('should handle conditional classes', () => {
    const isActive = true
    const result = cn('base-class', isActive && 'active-class')
    expect(result).toContain('base-class')
    expect(result).toContain('active-class')
  })

  it('should not add inactive conditional classes', () => {
    const isActive = false
    const result = cn('base-class', isActive && 'active-class')
    expect(result).toContain('base-class')
    expect(result).not.toContain('active-class')
  })

  it('should handle object notation', () => {
    const result = cn('base', { 'conditional': true, 'inactive': false })
    expect(result).toContain('base')
    expect(result).toContain('conditional')
    expect(result).not.toContain('inactive')
  })
})
