import { useAuth } from '@/core/auth/useAuth'
import { useTheme } from '@/core/theme/ThemeProvider'
import { Bell, Sun, Moon, LogOut, Menu, User } from 'lucide-react'
import { useState } from 'react'
import * as DropdownMenu from '@radix-ui/react-dropdown-menu'

interface HeaderProps {
  onMenuClick: () => void
}

export default function Header({ onMenuClick }: HeaderProps) {
  const { user, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const [showNotifications, setShowNotifications] = useState(false)

  return (
    <header className="sticky top-0 z-30 border-b border-border bg-card/95 backdrop-blur supports-[backdrop-filter]:bg-card/60">
      <div className="flex items-center justify-between px-4 sm:px-6 h-16">
        {/* Left side - Mobile menu button */}
        <div className="flex items-center gap-3">
          <button
            onClick={onMenuClick}
            className="lg:hidden p-2 rounded-md hover:bg-muted"
            aria-label="Open menu"
          >
            <Menu className="w-5 h-5" />
          </button>
          <div className="hidden sm:block">
            <p className="text-sm text-muted-foreground">
              {new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
            </p>
          </div>
        </div>

        {/* Right side */}
        <div className="flex items-center gap-2">
          {/* Theme toggle */}
          <button
            onClick={toggleTheme}
            className="p-2 rounded-md hover:bg-muted transition-colors"
            aria-label={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
          >
            {theme === 'dark' ? (
              <Sun className="w-5 h-5 text-yellow-400" />
            ) : (
              <Moon className="w-5 h-5 text-slate-600" />
            )}
          </button>

          {/* Notifications */}
          <button
            onClick={() => setShowNotifications(!showNotifications)}
            className="relative p-2 rounded-md hover:bg-muted transition-colors"
            aria-label="Notifications"
          >
            <Bell className="w-5 h-5" />
            <span className="absolute top-1 right-1 w-2 h-2 bg-destructive rounded-full" />
          </button>

          {/* User dropdown */}
          <DropdownMenu.Root>
            <DropdownMenu.Trigger asChild>
              <button className="flex items-center gap-2 p-2 rounded-md hover:bg-muted transition-colors">
                <div className="w-8 h-8 rounded-full bg-primary flex items-center justify-center">
                  <span className="text-xs font-bold text-primary-foreground">
                    {user?.displayName?.charAt(0).toUpperCase() || 'U'}
                  </span>
                </div>
                <div className="hidden md:block text-left">
                  <p className="text-sm font-medium leading-none">{user?.displayName || 'User'}</p>
                  <p className="text-xs text-muted-foreground leading-none mt-1">{user?.employeeId}</p>
                </div>
              </button>
            </DropdownMenu.Trigger>
            <DropdownMenu.Content align="end" className="w-56">
              <DropdownMenu.Label>My Account</DropdownMenu.Label>
              <DropdownMenu.Separator />
              <DropdownMenu.Item className="cursor-pointer">
                <User className="mr-2 h-4 w-4" />
                <span>Profile</span>
              </DropdownMenu.Item>
              <DropdownMenu.Item className="cursor-pointer">
                <Bell className="mr-2 h-4 w-4" />
                <span>Notifications</span>
              </DropdownMenu.Item>
              <DropdownMenu.Separator />
              <DropdownMenu.Item
                className="cursor-pointer text-destructive focus:text-destructive"
                onClick={logout}
              >
                <LogOut className="mr-2 h-4 w-4" />
                <span>Log out</span>
              </DropdownMenu.Item>
            </DropdownMenu.Content>
          </DropdownMenu.Root>
        </div>
      </div>
    </header>
  )
}
