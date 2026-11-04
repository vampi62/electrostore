import { describe, it, expect } from 'vitest'
import { checkAiFile, fileExtension, formatFileSize } from '@/helpers/aiAttachments'

const MB = 1024 * 1024

const limits = (overrides = {}) => ({
  maxCount: 5,
  imageMimeTypes: ['image/png', 'image/jpeg'],
  imageExtensions: ['.png', '.jpeg'],
  maxImageSizeMb: 8,
  textExtensions: ['.txt', '.csv'],
  maxTextSizeMb: 10,
  audio: { mimeTypes: ['audio/mpeg'], extensions: ['.mp3'], maxSizeMb: 25 },
  ...overrides,
})

const file = (name, type, size = 100) => ({ name, type, size })

describe('fileExtension', () => {
  it('is the lowercase extension with its dot', () => {
    expect(fileExtension('BOM.CSV')).toBe('.csv')
    expect(fileExtension('a.b.png')).toBe('.png')
  })

  it('is empty without extension', () => {
    expect(fileExtension('README')).toBe('')
  })
})

describe('checkAiFile', () => {
  it('decides a text file by its extension, whatever the mime type', () => {
    expect(checkAiFile(file('bom.csv', 'application/vnd.ms-excel'), limits())).toEqual({ kind: 'text' })
    expect(checkAiFile(file('notes.txt', ''), limits())).toEqual({ kind: 'text' })
  })

  it('accepts an image whose mime type and extension are both allowed', () => {
    expect(checkAiFile(file('board.png', 'image/png'), limits())).toEqual({ kind: 'image' })
  })

  it('refuses an image with a mime type or an extension that is not allowed', () => {
    expect(checkAiFile(file('board.png', 'application/octet-stream'), limits())).toEqual({ error: 'type' })
    expect(checkAiFile(file('board.gif', 'image/png'), limits())).toEqual({ error: 'type' })
  })

  it('accepts an audio file when the transcription is available', () => {
    expect(checkAiFile(file('voice.mp3', 'audio/mpeg'), limits())).toEqual({ kind: 'audio' })
  })

  it('refuses an audio file when the transcription is not available', () => {
    expect(checkAiFile(file('voice.mp3', 'audio/mpeg'), limits({ audio: null }))).toEqual({ error: 'type' })
  })

  it('refuses a file of another type', () => {
    expect(checkAiFile(file('program.exe', 'application/x-msdownload'), limits())).toEqual({ error: 'type' })
  })

  it('refuses an empty file', () => {
    expect(checkAiFile(file('empty.txt', 'text/plain', 0), limits())).toEqual({ error: 'empty' })
  })

  it('uses the size limit of the kind of file', () => {
    expect(checkAiFile(file('big.png', 'image/png', 8 * MB + 1), limits())).toEqual({ error: 'tooBig', maxSizeMb: 8 })
    expect(checkAiFile(file('big.txt', 'text/plain', 8 * MB + 1), limits())).toEqual({ kind: 'text' })
    expect(checkAiFile(file('big.csv', 'text/csv', 10 * MB + 1), limits())).toEqual({ error: 'tooBig', maxSizeMb: 10 })
    expect(checkAiFile(file('big.mp3', 'audio/mpeg', 25 * MB + 1), limits())).toEqual({ error: 'tooBig', maxSizeMb: 25 })
  })
})

describe('formatFileSize', () => {
  it('uses the unit that fits', () => {
    expect(formatFileSize(512)).toBe('512 B')
    expect(formatFileSize(2048)).toBe('2 KB')
    expect(formatFileSize(3 * MB)).toBe('3.0 MB')
  })
})
