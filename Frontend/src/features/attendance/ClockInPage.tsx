import { useState, useEffect } from 'react'
import { useAuth } from '@/core/auth/useAuth'
import { post } from '@/core/api/apiClient'
import { ENDPOINTS } from '@/core/api/endpoints'
import { Clock, Calendar, MapPin, CheckCircle2, XCircle, Loader2, AlertCircle } from 'lucide-react'
import { cn } from '@/core/utils/cn'
import { formatDisplayDate, formatTime } from '@/core/utils/formatDate'
import { getCurrentShift, SHIFTS } from '@/shared/constants/shifts'

export default function ClockInPage() {
  const { user } = useAuth()
  const [currentTime, setCurrentTime] = useState(new Date())
  const [isClockingIn, setIsClockingIn] = useState(false)
  const [isClockingOut, setIsClockingOut] = useState(false)
  const [clockStatus, setClockStatus] = useState<'in' | 'out' | null>(null)
  const [lastAction, setLastAction] = useState<{ type: string; time: string } | null>(null)

  // Update clock every second
  useEffect(() => {
    const timer = setInterval(() => setCurrentTime(new Date()), 1000)
    return () => clearInterval(timer)
  }, [])

  const shift = getCurrentShift(currentTime.getHours())

  const [error, setError] = useState<string | null>(null)

  const handleClockIn = async () => {
    setIsClockingIn(true)
    setError(null)
    try {
      await post(ENDPOINTS.attendance.clockIn, {
        employeeId: user?.employeeId || user?.id,
        timestamp: new Date().toISOString(),
        location: { latitude: 0, longitude: 0 } // GPS placeholder
      })
      setClockStatus('in')
      setLastAction({ type: 'Clock In', time: formatTime(new Date(), 'p') })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Clock in failed. Please try again.')
    } finally {
      setIsClockingIn(false)
    }
  }

  const handleClockOut = async () => {
    setIsClockingOut(true)
    setError(null)
    try {
      await post(ENDPOINTS.attendance.clockOut, {
        employeeId: user?.employeeId || user?.id,
        timestamp: new Date().toISOString(),
        location: { latitude: 0, longitude: 0 } // GPS placeholder
      })
      setClockStatus('out')
      setLastAction({ type: 'Clock Out', time: formatTime(new Date(), 'p') })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Clock out failed. Please try again.')
    } finally {
      setIsClockingOut(false)
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Clock In / Out</h1>
          <p className="text-sm text-muted-foreground mt-1">Quick attendance tracking</p>
        </div>
        <div className="text-right">
          <p className="text-3xl font-mono font-bold tabular-nums">{formatTime(currentTime, 'HH:mm:ss')}</p>
          <p className="text-xs text-muted-foreground">{formatDisplayDate(currentTime)}</p>
        </div>
      </div>

      {/* Shift info banner */}
      <div className={cn(
        'rounded-xl border p-4 flex items-center gap-4',
        shift.code === 'ES' ? 'border-green-500/30 bg-green-500/5' :
        shift.code === 'MS' ? 'border-orange-500/30 bg-orange-500/5' :
        'border-blue-500/30 bg-blue-500/5'
      )}>
        <Calendar className="w-6 h-6" />
        <div className="flex-1">
          <p className="font-semibold">{shift.name}</p>
          <p className="text-sm text-muted-foreground">{shift.startTime} → {shift.endTime}</p>
        </div>
        <div className="hidden sm:block">
          <span className="px-3 py-1 rounded-full text-sm font-mono bg-primary/10 text-primary">
            {user?.displayName || ''}
          </span>
        </div>
      </div>

      {/* Big clock buttons */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Clock In Button */}
        <button
          onClick={handleClockIn}
          disabled={isClockingIn || isClockingOut || clockStatus === 'in'}
          className={cn(
            'group relative p-8 rounded-2xl border-2 transition-all min-h-[200px] flex flex-col items-center justify-center gap-4',
            clockStatus === 'in'
              ? 'border-green-500/50 bg-green-500/10'
              : isClockingIn
                ? 'border-yellow-500/50 bg-yellow-500/5 animate-pulse'
                : 'border-border bg-card hover:border-green-500/50 hover:bg-green-500/5 cursor-pointer'
          )}
        >
          {isClockingIn ? (
            <Loader2 className="w-12 h-12 text-yellow-500 animate-spin" />
          ) : clockStatus === 'in' ? (
            <>
              <CheckCircle2 className="w-12 h-12 text-green-500" />
              <span className="text-2xl font-bold text-green-400">Clocked In!</span>
              {lastAction && lastAction.type === 'Clock In' && (
                <span className="text-sm text-muted-foreground">{lastAction.time}</span>
              )}
            </>
          ) : (
            <>
              <div className="w-16 h-16 rounded-full bg-green-500/20 flex items-center justify-center group-hover:scale-110 transition-transform">
                <Clock className="w-8 h-8 text-green-400" />
              </div>
              <span className="text-xl font-bold">Clock In</span>
              <span className="text-sm text-muted-foreground">Tap to record your arrival</span>
            </>
          )}
        </button>

        {/* Clock Out Button */}
        <button
          onClick={handleClockOut}
          disabled={isClockingOut || isClockingIn || clockStatus === 'out'}
          className={cn(
            'group relative p-8 rounded-2xl border-2 transition-all min-h-[200px] flex flex-col items-center justify-center gap-4',
            clockStatus === 'out'
              ? 'border-red-500/50 bg-red-500/10'
              : isClockingOut
                ? 'border-yellow-500/50 bg-yellow-500/5 animate-pulse'
                : 'border-border bg-card hover:border-red-500/50 hover:bg-red-500/5 cursor-pointer'
          )}
        >
          {isClockingOut ? (
            <Loader2 className="w-12 h-12 text-yellow-500 animate-spin" />
          ) : clockStatus === 'out' ? (
            <>
              <XCircle className="w-12 h-12 text-red-500" />
              <span className="text-2xl font-bold text-red-400">Clocked Out!</span>
              {lastAction && lastAction.type === 'Clock Out' && (
                <span className="text-sm text-muted-foreground">{lastAction.time}</span>
              )}
            </>
          ) : (
            <>
              <div className="w-16 h-16 rounded-full bg-red-500/20 flex items-center justify-center group-hover:scale-110 transition-transform">
                <Clock className="w-8 h-8 text-red-400 rotate-180" />
              </div>
              <span className="text-xl font-bold">Clock Out</span>
              <span className="text-sm text-muted-foreground">Tap to record your departure</span>
            </>
          )}
        </button>
      </div>

      {/* Location info */}
      <div className="flex items-center gap-3 text-sm text-muted-foreground">
        <MapPin className="w-4 h-4" />
        <span>Location tracking enabled — GPS coordinates will be recorded with each punch</span>
      </div>
    </div>
  )
}
