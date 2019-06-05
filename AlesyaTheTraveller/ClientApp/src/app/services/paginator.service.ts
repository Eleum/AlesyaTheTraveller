import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class PaginatorService {

  constructor() { }

  getPaginator(totalItems: number, current: number = 1, pageSize: number = 6) {
    let totalPages = Math.ceil(totalItems / pageSize);

    if (current < 1) {
      current = 1;
    } else if (current > totalPages) {
      current = totalPages;
    }

    let startPage: number, endPage: number;
    if (totalPages <= 10) {
      // less than 10 total pages so show all
      startPage = 1;
      endPage = totalPages;
    } else {
      // more than 10 total pages so calculate start and end pages
      if (current <= 6) {
        startPage = 1;
        endPage = 10;
      } else if (current + 4 >= totalPages) {
        startPage = totalPages - 9;
        endPage = totalPages;
      } else {
        startPage = current - 5;
        endPage = current + 4;
      }
    }

    // calculate start and end item indexes
    let startIndex = (current - 1) * pageSize;
    let endIndex = Math.min(startIndex + pageSize - 1, totalItems - 1);

    // create an array of pages to ng-repeat in the pager control
    let pages = Array.from(Array((endPage + 1) - startPage).keys()).map(i => startPage + i);

    // return object with all pager properties required by the view
    return {
      totalItems: totalItems,
      currentPage: current,
      pageSize: pageSize,
      totalPages: totalPages,
      startPage: startPage,
      endPage: endPage,
      startIndex: startIndex,
      endIndex: endIndex,
      pages: pages
    };
  }
}
