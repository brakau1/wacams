import { useState, useRef } from 'react'
import { Upload, FileText, X, CheckCircle2, Loader2 } from 'lucide-react'
import { cn } from '@/core/utils/cn'

interface SignedFormUploaderProps {
  onUpload: (file: File) => Promise<void>
  existingUrl?: string | null
}

export default function SignedFormUploader({ onUpload, existingUrl }: SignedFormUploaderProps) {
  const [dragging, setDragging] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [uploadedFile, setUploadedFile] = useState<File | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setDragging(false)
    const dropped = e.dataTransfer.files[0]
    if (dropped && dropped.type === 'application/pdf') {
      setUploadedFile(dropped)
    }
  }

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0]
    if (selected) setUploadedFile(selected)
  }

  const handleUpload = async () => {
    if (!uploadedFile) return
    setUploading(true)
    try {
      await onUpload(uploadedFile)
    } finally {
      setUploading(false)
    }
  }

  return (
    <div className="space-y-3">
      {/* Existing signed form */}
      {existingUrl && (
        <div className="flex items-center gap-3 p-3 rounded-lg bg-green-500/5 border border-green-500/20">
          <CheckCircle2 className="w-5 h-5 text-green-400 flex-shrink-0" />
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium">Signed form already uploaded</p>
            <a href={existingUrl} target="_blank" rel="noopener noreferrer" className="text-xs text-primary hover:underline">
              View signed form →
            </a>
          </div>
        </div>
      )}

      {/* Upload area */}
      <div
        onDragOver={(e) => { e.preventDefault(); setDragging(true) }}
        onDragLeave={() => setDragging(false)}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
        className={cn(
          'border-2 border-dashed rounded-lg p-6 text-center cursor-pointer transition-colors',
          dragging ? 'border-primary bg-primary/5' : 'border-border hover:border-primary/50'
        )}
      >
        <Upload className="w-8 h-8 text-muted-foreground mx-auto mb-2" />
        <p className="text-sm font-medium">
          Drag & drop signed warning form PDF here
        </p>
        <p className="text-xs text-muted-foreground mt-1">or click to browse — PDF only</p>
        <input
          ref={fileInputRef}
          type="file"
          accept=".pdf"
          onChange={handleFileSelect}
          className="hidden"
        />
      </div>

      {/* Selected file */}
      {uploadedFile && (
        <div className="flex items-center justify-between p-3 rounded-lg bg-muted/50 border border-border">
          <div className="flex items-center gap-3">
            <FileText className="w-5 h-5 text-primary" />
            <div>
              <p className="text-sm font-medium">{uploadedFile.name}</p>
              <p className="text-xs text-muted-foreground">{(uploadedFile.size / 1024).toFixed(1)} KB</p>
            </div>
          </div>
          <button
            onClick={(e) => { e.stopPropagation(); setUploadedFile(null) }}
            className="p-1 rounded-md hover:bg-muted"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Upload button */}
      {uploadedFile && (
        <button
          onClick={handleUpload}
          disabled={uploading}
          className="w-full px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 disabled:opacity-50 flex items-center justify-center gap-2"
        >
          {uploading && <Loader2 className="w-4 h-4 animate-spin" />}
          {uploading ? 'Uploading...' : 'Upload Signed Form'}
        </button>
      )}
    </div>
  )
}
