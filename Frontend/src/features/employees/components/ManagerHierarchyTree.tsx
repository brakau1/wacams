import { cn } from '@/core/utils/cn'
import { ChevronRight, User, Users } from 'lucide-react'

interface TreeNode {
  id: string
  displayName: string
  role: string
  children?: TreeNode[]
}

interface ManagerHierarchyTreeProps {
  root: TreeNode
  selectedId?: string
  onNodeClick?: (node: TreeNode) => void
}

export default function ManagerHierarchyTree({ root, selectedId, onNodeClick }: ManagerHierarchyTreeProps) {
  return (
    <div className="space-y-1">
      <TreeNodeComponent node={root} level={0} selectedId={selectedId} onNodeClick={onNodeClick} />
    </div>
  )
}

function TreeNodeComponent({ node, level, selectedId, onNodeClick }: {
  node: TreeNode
  level: number
  selectedId?: string
  onNodeClick?: (node: TreeNode) => void
}) {
  const hasChildren = node.children && node.children.length > 0
  const isSelected = node.id === selectedId

  return (
    <div>
      <button
        onClick={() => onNodeClick?.(node)}
        className={cn(
          'flex items-center gap-2 w-full px-3 py-2 rounded-lg text-left transition-colors',
          isSelected ? 'bg-primary/10 text-primary font-medium' : 'hover:bg-muted/50 text-foreground'
        )}
        style={{ paddingLeft: `${level * 16 + 12}px` }}
      >
        {hasChildren ? (
          <ChevronRight className="w-4 h-4 text-muted-foreground flex-shrink-0" />
        ) : (
          <span className="w-4" />
        )}
        {hasChildren ? (
          <Users className="w-4 h-4 text-muted-foreground flex-shrink-0" />
        ) : (
          <User className="w-4 h-4 text-muted-foreground flex-shrink-0" />
        )}
        <span className="text-sm truncate">{node.displayName}</span>
        <span className="ml-auto text-xs text-muted-foreground hidden sm:inline">{node.role}</span>
      </button>

      {hasChildren && (
        <div className="mt-1 space-y-1">
          {node.children!.map(child => (
            <TreeNodeComponent
              key={child.id}
              node={child}
              level={level + 1}
              selectedId={selectedId}
              onNodeClick={onNodeClick}
            />
          ))}
        </div>
      )}
    </div>
  )
}
