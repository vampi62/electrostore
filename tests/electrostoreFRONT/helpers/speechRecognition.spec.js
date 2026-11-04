import { describe, it, expect, vi, afterEach } from 'vitest'
import { getSpeechRecognition, hasAudioInput, micErrorKey, localSpeechStatus, installLocalSpeech, recognitionLang, transcriptOf, joinSpeechText } from '@/helpers/speechRecognition'

const result = (transcript, isFinal) => Object.assign([{ transcript }], { isFinal })

describe('getSpeechRecognition', () => {
  afterEach(() => {
    delete window.SpeechRecognition
    delete window.webkitSpeechRecognition
  })

  it('is null when the browser has no speech recognition', () => {
    expect(getSpeechRecognition()).toBeNull()
  })

  it('uses the prefixed implementation when it is the only one', () => {
    window.webkitSpeechRecognition = function Prefixed() {}
    expect(getSpeechRecognition()).toBe(window.webkitSpeechRecognition)
  })

  it('prefers the standard implementation', () => {
    window.SpeechRecognition = function Standard() {}
    window.webkitSpeechRecognition = function Prefixed() {}
    expect(getSpeechRecognition()).toBe(window.SpeechRecognition)
  })
})

describe('localSpeechStatus', () => {
  it('is unknown without recognition or without the availability api', async () => {
    expect(await localSpeechStatus('fr-FR', null)).toBe('unknown')
    expect(await localSpeechStatus('fr-FR', function Recognition() {})).toBe('unknown')
  })

  it('asks the browser about the on-device recognition of the language', async () => {
    const available = vi.fn().mockResolvedValue('downloadable')
    expect(await localSpeechStatus('fr-FR', { available })).toBe('downloadable')
    expect(available).toHaveBeenCalledWith({ langs: ['fr-FR'], processLocally: true })
  })

  it('is unknown for a status it does not know or when the browser fails', async () => {
    expect(await localSpeechStatus('fr-FR', { available: vi.fn().mockResolvedValue('whatever') })).toBe('unknown')
    expect(await localSpeechStatus('fr-FR', { available: vi.fn().mockRejectedValue(new Error('boom')) })).toBe('unknown')
  })
})

describe('installLocalSpeech', () => {
  it('cannot install without the install api', async () => {
    expect(await installLocalSpeech('fr-FR', null)).toBe(false)
    expect(await installLocalSpeech('fr-FR', function Recognition() {})).toBe(false)
  })

  it('downloads the language pack', async () => {
    const install = vi.fn().mockResolvedValue(true)
    expect(await installLocalSpeech('fr-FR', { install })).toBe(true)
    expect(install).toHaveBeenCalledWith({ langs: ['fr-FR'], processLocally: true })
  })

  it('is false when the download fails', async () => {
    expect(await installLocalSpeech('fr-FR', { install: vi.fn().mockRejectedValue(new Error('boom')) })).toBe(false)
  })
})

describe('recognitionLang', () => {
  it('keeps the region of the browser when it speaks the language of the application', () => {
    expect(recognitionLang('en', 'en-GB')).toBe('en-GB')
    expect(recognitionLang('fr', 'fr-CA')).toBe('fr-CA')
  })

  it('falls back to a default region of the language of the application', () => {
    expect(recognitionLang('fr', 'en-US')).toBe('fr-FR')
    expect(recognitionLang('en', '')).toBe('en-US')
  })

  it('uses the locale as is when the language has no default region', () => {
    expect(recognitionLang('de', 'en-US')).toBe('de')
  })
})

describe('transcriptOf', () => {
  it('separates the final words from the ones still being guessed', () => {
    expect(transcriptOf([result('hello ', true), result('wor', false)])).toEqual({ final: 'hello ', interim: 'wor' })
  })

  it('is empty without result', () => {
    expect(transcriptOf([])).toEqual({ final: '', interim: '' })
  })
})

describe('joinSpeechText', () => {
  it('adds the spoken words after the typed text', () => {
    expect(joinSpeechText('create a tag ', ' red ')).toBe('create a tag red')
  })

  it('keeps the typed text when nothing is said', () => {
    expect(joinSpeechText('typed', '  ')).toBe('typed')
  })

  it('uses the spoken words alone when nothing is typed', () => {
    expect(joinSpeechText('', 'hello')).toBe('hello')
  })
})

describe('micErrorKey', () => {
  it('tells a missing microphone from a refused access', () => {
    expect(micErrorKey('NotFoundError')).toBe('common.VAiMicMissing')
    expect(micErrorKey('audio-capture')).toBe('common.VAiMicMissing')
    expect(micErrorKey('NotAllowedError')).toBe('common.VAiMicDenied')
    expect(micErrorKey('not-allowed')).toBe('common.VAiMicDenied')
  })

  it('tells a busy microphone and any other failure', () => {
    expect(micErrorKey('NotReadableError')).toBe('common.VAiMicBusy')
    expect(micErrorKey('InvalidStateError')).toBe('common.VAiMicError')
    expect(micErrorKey('')).toBe('common.VAiMicError')
  })
})

describe('hasAudioInput', () => {
  const setDevices = (value) => Object.defineProperty(navigator, 'mediaDevices', { value, configurable: true })

  afterEach(() => setDevices(undefined))

  it('is true when the browser cannot list the devices', async () => {
    setDevices(undefined)
    expect(await hasAudioInput()).toBe(true)
  })

  it('is false without any audio input', async () => {
    setDevices({ enumerateDevices: async () => [{ kind: 'videoinput' }, { kind: 'audiooutput' }] })
    expect(await hasAudioInput()).toBe(false)
  })

  it('is true with an audio input', async () => {
    setDevices({ enumerateDevices: async () => [{ kind: 'audioinput' }] })
    expect(await hasAudioInput()).toBe(true)
  })

  it('is true when the listing fails', async () => {
    setDevices({ enumerateDevices: async () => { throw new Error('boom') } })
    expect(await hasAudioInput()).toBe(true)
  })
})
