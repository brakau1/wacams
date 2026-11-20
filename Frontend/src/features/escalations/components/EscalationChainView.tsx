import { cn } from '@/core/utils/cn'
import { ArrowRight, CheckCircle2, Clock, AlertTriangle, XCircle } from 'lucide-react'

interface ChainNode {
  id: string
  name: string
  role: string
  status: 'completed' | 'current' | 'pending' | 'expired'
  respondedAt?: string
}

interface EscalationChainViewProps {
  chain: ChainNode[]
}

export default function EscalationChainView({ chain }: EscalationChainViewProps) {
  return (
    <div className="rounded-xl border border-border bg-card p-5">
      <h3 className="font-semibold mb-4">Escalation Chain</h3>
      <div className="space-y-0">
        {chain.map((node, index) => (
          <div key={node.id} className="flex items-center">
            {/* Node */}
            <div className={cn(
              'w-10 h-10 rounded-full flex items-center justify-center flex-shrink-0 border-2',
              node.status === 'completed' ? 'bg-green-500/10 border-green-500 text-green-400' :
              node.status === 'current' ? 'bg-primary/10 border-primary text-primary animate-pulse' :
              node.status === 'expired' ? 'bg-red-500/10 border-red-500 text-red-400' :
              'bg-muted/30 border-muted text-muted-foreground'
            )}>
              {node.status === 'completed' && <CheckCircle2 className="w-5 h-5" />}
              {node.status === 'current' && <Clock className="w-5 h-5" />}
              {node.status === 'expired' && <AlertTriangle className="w-5 h-5" />}
              {node.status === 'pending' && <XCircle className="w-5 h-5 opacity-50" />}
            </div>

            {/* Info */}
            <div className="ml-3 flex-1">
              <p className="text-sm font-medium">{node.name}</p>
              <p className="text-xs text-muted-foreground">{node.role}</p>
              {node.respondedAt && (
                <p className="text-xs text-green-400">Responded: {new Date(node.respondedAt).toLocaleDateString()}</p>
              )}
            </div>

            {/* Connector */}
            {index < chain.length - 1 && (
              <ArrowRight className="w-4 h-4 text-muted-foreground mx-3 flex-shrink-0" />
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
