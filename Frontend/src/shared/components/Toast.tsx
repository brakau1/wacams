import { useState, useEffect } from 'react'
import { cn } from '@/core/utils/cn'
import { CheckCircle, AlertCircle, XCircle, Info, X } from 'lucide-react'

export type ToastType = 'success' | 'error' | 'warning' | 'info'

interface ToastProps {
  id: string
  type: ToastType
  title: string
  message?: string
  duration?: number
  onClose: (id: string) => void
}

const TOAST_CONFIG = {
  success: { icon: CheckCircle, color: 'text-green-500', bg: 'border-green-500/20', bgLight: 'bg-green-500/10' },
  error: { icon: XCircle, color: 'text-red-500', bg: 'border-red-500/20', bgLight: 'bg-red-500/10' },
  warning: { icon: AlertCircle, color: 'text-yellow-500', bg: 'border-yellow-500/20', bgLight: 'bg-yellow-500/10' },
  info: { icon: Info, color: 'text-blue-500', bg: 'border-blue-500/20', bgLight: 'bg-blue-500/10' }
}

export default function Toast({ id, type, title, message, duration = 4000, onClose }: ToastProps) {
  const config = TOAST_CONFIG[type]
  const Icon = config.icon

  useEffect(() => {
    const timer = setTimeout(() => onClose(id), duration)
    return () => clearTimeout(timer)
  }, [id, duration, onClose])

  return (
    <div className={cn(
      'flex items-start gap-3 p-4 rounded-lg border shadow-lg animate-fade-in',
      config.bg,
      'max-w-md w-full'
    )}>
      <Icon className={cn('w-5 h-5 flex-shrink-0 mt-0.5', config.color)} />
      <div className="flex-1 min-w-0">
        <p className="text-sm font-semibold">{title}</p>
        {message && <p className="text-xs text-muted-foreground mt-0.5">{message}</p>}
      </div>
      <button onClick={() => onClose(id)} className="p-1 rounded hover:bg-muted/50 flex-shrink-0">
        <X className="w-4 h-4" />
      </button>
    </div>
  )
}

// ─── Toast Manager ────────────────────────────────────────────────────────

interface ToastMessage {
  id: string
  type: ToastType
  title: string
  message?: string
}

let toasts: ToastMessage[] = []
let listeners: Set<(toasts: ToastMessage[]) => void> = new Set()

export const toast = {
  success: (title: string, message?: string) => addToast('success', title, message),
  error: (title: string, message?: string) => addToast('error', title, message),
  warning: (title: string, message?: string) => addToast('warning', title, message),
  info: (title: string, message?: string) => addToast('info', title, message),
  remove: (id: string) => {
    toasts = toasts.filter(t => t.id !== id)
    notify()
  }
}

function addToast(type: ToastType, title: string, message?: string): string {
  const id = Date.now().toString(36) + Math.random().toString(36).slice(2)
  toasts.push({ id, type, title, message })
  notify()
  return id
}

function notify(): void {
  listeners.forEach(fn => fn([...toasts]))
}

export function ToastContainer() {
  const [currentToasts, setCurrentToasts] = useState<ToastMessage[]>([])

  useEffect(() => {
    listeners.add(setCurrentToasts)
    return () => {
      listeners.delete(setCurrentToasts)
    }
  }, [])

  return (
    <div className="fixed bottom-4 right-4 z-[70] flex flex-col gap-2 max-h-[80vh] overflow-y-auto">
      {currentToasts.map(t => (
        <Toast
          key={t.id}
          id={t.id}
          type={t.type}
          title={t.title}
          message={t.message}
          onClose={toast.remove}
        />
      ))}
    </div>
  )
}
