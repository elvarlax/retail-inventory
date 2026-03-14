interface Props {
  page: number
  pageSize: number
  total: number
  onPage: (page: number) => void
}

export default function Pagination({ page, pageSize, total, onPage }: Props) {
  const totalPages = Math.ceil(total / pageSize)

  if (totalPages <= 1) return null

  const pages = getVisiblePages(page, totalPages)

  return (
    <div className="pagination">
      <button disabled={page === 1} onClick={() => onPage(page - 1)} aria-label="Previous page">&lt;</button>
      {pages.map((item, index) => (
        item === 'ellipsis'
          ? <span key={`ellipsis-${index}`} className="ellipsis">...</span>
          : (
            <button
              key={item}
              className={item === page ? 'active' : ''}
              onClick={() => onPage(item)}
              aria-label={`Go to page ${item}`}>
              {item}
            </button>
          )
      ))}
      <button disabled={page === totalPages} onClick={() => onPage(page + 1)} aria-label="Next page">&gt;</button>
    </div>
  )
}

function getVisiblePages(page: number, totalPages: number): Array<number | 'ellipsis'> {
  if (totalPages <= 10) {
    return Array.from({ length: totalPages }, (_, index) => index + 1)
  }

  if (page <= 5) {
    return [1, 2, 3, 4, 5, 6, 7, 'ellipsis', totalPages]
  }

  if (page >= totalPages - 4) {
    return [1, 'ellipsis', totalPages - 6, totalPages - 5, totalPages - 4, totalPages - 3, totalPages - 2, totalPages - 1, totalPages]
  }

  return [1, 'ellipsis', page - 2, page - 1, page, page + 1, page + 2, 'ellipsis', totalPages]
}
