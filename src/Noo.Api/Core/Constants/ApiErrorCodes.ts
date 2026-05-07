const ApiErrorCodes: Record<
  string,
  {
    name: string
    description: string
  }
> = {
  ALREADY_EXISTS: {
    name: 'Неизвестная ошибка',
    description: 'Попробуйте позже или свяжитесь с поддержкой'
  },
  BAD_REQUEST: {
    name: 'Некорректный запрос',
    description: 'Проверьте правильность введенных данных'
  },
  CANT_CHANGE_ROLE: {
    name: 'Невозможно изменить роль',
    description: 'Изменить роль можно только у пользователей с ролью \"ученик\"'
  },
  CANT_SEND_EMAIL: {
    name: 'Неизвестная ошибка',
    description: 'Попробуйте позже или свяжитесь с поддержкой'
  },
  FORBIDDEN: {
    name: 'Доступ запрещен',
    description: 'У вас нет прав доступа к этому ресурсу'
  },
  NOT_FOUND: {
    name: 'Не найдено',
    description: 'Запрашиваемый ресурс не найден'
  },
  NO_MENTOR_ASSIGNED: {
    name: 'Неизвестная ошибка',
    description: 'Попробуйте позже или свяжитесь с поддержкой'
  },
  TOKEN_EXPIRED: {
    name: 'Неизвестная ошибка',
    description: 'Попробуйте позже или свяжитесь с поддержкой'
  },
  UNAUTHORIZED: {
    name: 'Запрос не авторизован',
    description: 'Пожалуйста, войдите в систему, чтобы продолжить'
  },
  UNSUPPORTED_MEDIA_TYPE: {
    name: 'Неизвестная ошибка',
    description: 'Попробуйте позже или свяжитесь с поддержкой'
  },
  USER_ALREADY_VOTED: {
    name: 'Неизвестная ошибка',
    description: 'Попробуйте позже или свяжитесь с поддержкой'
  },
  USER_IS_BLOCKED: {
    name: 'Пользователь заблокирован',
    description: 'Обратитесь в поддержку для получения дополнительной информации'
  },
  USER_NOT_VERIFIED: {
    name: 'Пользователь не подтвережден',
    description: 'Пожалуйста, подтвердите свою почту для доступа к платформе'
  },
  fallback: {
    name: 'Неизвестная ошибка',
    description: 'Попробуйте позже или проверьте подключение к интернету'
  }
} as const

export { ApiErrorCodes }
