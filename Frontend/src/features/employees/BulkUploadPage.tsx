import { useState, useRef } from 'react'
import { Upload, FileSpreadsheet, X, CheckCircle2, AlertTriangle } from 'lucide-react'
import { cn } from '@/core/utils/cn'

export default function BulkUploadPage() {
  const [file, setFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [result, setResult] = useState<{ success: boolean; total: number; errors: string[] } | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    const droppedFile = e.dataTransfer.files[0]
    if (droppedFile && (droppedFile.name.endsWith('.csv') || droppedFile.name.endsWith('.xlsx'))) {
      setFile(droppedFile)
    }
  }

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0]
    if (selectedFile) setFile(selectedFile)
  }

  const handleUpload = async () => {
    if (!file) return
    setUploading(true)
    // Simulate upload
    setTimeout(() => {
      setResult({
        success: true,
        total: 15,
        errors: ['Row 3: Invalid employee ID format', 'Row 8: Duplicate email']
      })
      setUploading(false)
    }, 2000)
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Bulk Employee Upload</h1>
        <p className="text-sm text-muted-foreground mt-1">Import employees via CSV or Excel file</p>
      </div>

      {/* Template download */}
      <div className="rounded-xl border border-border bg-card p-5">
        <h3 className="font-semibold mb-3">Download Template</h3>
        <p className="text-sm text-muted-foreground mb-3">Use this template for correct column format:</p>
        <button className="flex items-center gap-2 px-4 py-2 rounded-lg bg-primary/10 text-primary text-sm font-medium hover:bg-primary/20 transition-colors">
          <FileSpreadsheet className="w-4 h-4" />
          Download CSV Template
        </button>
      </div>

      {/* Upload area */}
      <div className="rounded-xl border-2 border-dashed border-border bg-card p-8">
        {!file ? (
          <div
            onDragOver={(e) => e.preventDefault()}
            onDrop={handleDrop}
            onClick={() => fileInputRef.current?.click()}
            className="flex flex-col items-center justify-center py-12 cursor-pointer hover:border-primary/50 transition-colors"
          >
            <Upload className="w-12 h-12 text-muted-foreground mb-4" />
            <p className="text-sm font-medium mb-1">Drop your CSV or Excel file here</p>
            <p className="text-xs text-muted-foreground">or click to browse</p>
            <input
              ref={fileInputRef}
              type="file"
              accept=".csv,.xlsx,.xls"
              onChange={handleFileSelect}
              className="hidden"
            />
          </div>
        ) : (
          <div className="flex items-center justify-between p-4 rounded-lg bg-muted/50">
            <div className="flex items-center gap-3">
              <FileSpreadsheet className="w-8 h-8 text-primary" />
              <div>
                <p className="font-medium">{file.name}</p>
                <p className="text-xs text-muted-foreground">{(file.size / 1024).toFixed(1)} KB</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setFile(null)}
                className="p-2 rounded-md hover:bg-muted transition-colors"
              >
                <X className="w-4 h-4" />
              </button>
              <button
                onClick={handleUpload}
                disabled={uploading}
                className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 disabled:opacity-50"
              >
                {uploading ? 'Uploading...' : 'Upload'}
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Upload result */}
      {result && (
        <div className={cn(
          'rounded-xl border p-5',
          result.success ? 'border-green-500/20 bg-green-500/5' : 'border-red-500/20 bg-red-500/5'
        )}>
          <div className="flex items-center gap-3 mb-4">
            {result.success ? (
              <CheckCircle2 className="w-6 h-6 text-green-500" />
            ) : (
              <AlertTriangle className="w-6 h-6 text-red-500" />
            )}
            <div>
              <h3 className="font-semibold">{result.success ? 'Upload Complete' : 'Upload Failed'}</h3>
              <p className="text-sm text-muted-foreground">{result.total} rows processed</p>
            </div>
          </div>

          {result.errors.length > 0 && (
            <div className="mt-4 space-y-2">
              <p className="text-sm font-medium text-destructive">Errors ({result.errors.length}):</p>
              <ul className="space-y-1">
                {result.errors.map((err, i) => (
                  <li key={i} className="text-xs text-muted-foreground bg-destructive/5 px-3 py-2 rounded">
                    {err}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
